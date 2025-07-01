using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Awaken.TG.Assets.Modding;
using Awaken.TG.Debugging.Logging;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.ActionLogs;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Combat;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Saving.Cloud;
using Awaken.TG.Main.Saving.Cloud.Conflicts;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Audio;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.PatchNotes;
using Awaken.TG.Main.UI.UITooltips;
using Awaken.TG.Main.Utility.Patchers;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility;
using Awaken.Utility.Profiling;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Cursor = Awaken.TG.Main.UI.Cursors.Cursor;
using Debug = UnityEngine.Debug;
using Log = Awaken.Utility.Debugging.Log;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    /// <summary>
    /// The class responsible for setting up application services and views.
    /// Happens only once in application lifetime.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public class ApplicationScene : MonoBehaviour {
        public const string IsGoG = "IsGoG";
        string AppSceneInitializationID => $"{nameof(ApplicationScene)} init";
        
        // === Public references set in editor
        [Required] public ServicesReferences servicesRefs;
        [Required] public CommonReferences commonReferences;
        [Required] public DebugReferences debugReferences;
        [Required] public DebugFishing debugFishing;

        [Required] public GameConstants gameConstants;
        [Required] public ActorsRegister actorsRegister;
        [Required] public AutoAchievementsService autoAchievementsService;

        [Required] public SpecialPostProcessService postProcessService;
        [InlineEditor] public PatchNotesContainer patchNotesContainer;
        
        // === UI
        [Required, SerializeField] UIInitializer uiInitializer;
        
        static bool s_initCompleted;

        List<ICloudSyncResult> _cloudSyncFailures = new();
        SceneInitializer _sceneInitialization = new();
        List<Story> _allStoriesBuffer = new(8);

        Services Services => World.Services;
        
        public static void EDITOR_RuntimeReset() {
            s_initCompleted = false;
            //ReInput.Reset();
        }

        public static async UniTask WaitForAppInit() {
            if (World.Services != null) return;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            SceneManager.LoadScene(nameof(ApplicationScene), LoadSceneMode.Additive);
            // wait 2 frames to make sure that Start was invoked and everything was initialized
            await UniTask.DelayFrame(2);
            // wait until completed
            await UniTask.WaitUntil(() => s_initCompleted);
        }
        
        // === Initialization
        void Start() {
#if REMOTE_CONSOLE
            new Awaken.TG.Assets.Code.Debugging.RemoteConsole();
#endif
            // --- If on PC, not on GoG and not on Steam, then restart the app with steam.
/*#if UNITY_STANDALONE
            if (!Configuration.GetBool(IsGoG) && Steamworks.SteamAPI.RestartAppIfNecessary(
                    HeathenEngineering.SteamworksIntegration.SteamSettings.current.applicationId)) {
                TitleScreenUI.Exit();
                return;
            }
#endif*/
            
            Application.quitting -= ApplicationQuitting;
            Application.quitting += ApplicationQuitting;
            ClearScenesProcessorsCache();
            ProfilerValues.StartNewSession();
            try {
                UnityServices.InitializeAsync();
            } catch (Exception e) {
                Debug.LogException(e);
            }
            TitleScreenUtils.ForceRandomCharacterCreatorPreset();
            InitAll().Forget();
        }

        async UniTaskVoid InitAll() {
            _sceneInitialization.Clear();
            _sceneInitialization.GetNewElement(AppSceneInitializationID);
            SetupJobWorkers();
            CheckReferences();
            InitServicesCrucialForCloudConflict();
            await InitCloud();
            await InitializeServices();
            uiInitializer.InitAfterServices();
            await InitializeWorld();
            InitializeWorldBasedServices();
            uiInitializer.InitAfterWorld();
            HandleFailedCloudSync();
            _sceneInitialization.CompleteElement(AppSceneInitializationID);
            s_initCompleted = true;
        }
        
        void SetupJobWorkers() {
            JobsUtility.JobWorkerCount = math.min(8, JobsUtility.JobWorkerMaximumCount);
        }

        [Conditional("DEBUG")]
        void CheckReferences() {
            List<FieldInfo> unassignedFields = new();
            foreach (FieldInfo field in GetType().GetFields().Where(f => typeof(Object).IsAssignableFrom(f.FieldType))) {
                if (field.GetCustomAttribute<RequiredAttribute>() != null && field.GetValue(this) == null) {
                    unassignedFields.Add(field);
                }
            }

            if (unassignedFields.Any()) {
                string listOfFields = string.Join("\n", unassignedFields.Select(f => f.Name));
                throw new UnassignedReferenceException($"Unassigned references in root scene object. List of them:\n{listOfFields}");
            }
        }

        // === Cloud Sync
        async UniTask InitCloud() {
            CloudService.Initialize();
            await CloudService.Get.WaitForManagerInitialization();

            CloudService.Get.BeginSaveBatch();
            List<ICloudSyncResult> results = CloudService.Get.InitCloud()?.ToList();
            
            SocialService.SetCurrentGameLanguage();
            Log.Marking?.Warning($"Init Game Language: {LocalizationSettings.SelectedLocale.LocaleName}, loading tables.");
            LocalizationHelper.ForceLoadTables();
            
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool("debug.cloud.conflict", false)) {
                await ResolveCloudConflict(DateTime.Now, DateTime.Now);
                CloudService.Get.EndSaveBatch();
                return;
            }
#endif
            
            if (results == null) {
                CloudService.Get.EndSaveBatch();
                return;
            }

            CloudConflictResolution resolution = CloudConflictResolution.None;

            DateTime lastLocalTimestamp = DateTime.MinValue;
            DateTime lastCloudTimestamp = DateTime.MinValue;
            foreach (var conflictBetweenLocalAndCloud in results.OfType<ConflictBetweenLocalAndCloud>()) {
                if (lastLocalTimestamp < conflictBetweenLocalAndCloud.LocalTimeStamp) {
                    lastLocalTimestamp = conflictBetweenLocalAndCloud.LocalTimeStamp;
                }

                if (lastCloudTimestamp < conflictBetweenLocalAndCloud.CloudTimeStamp) {
                    lastCloudTimestamp = conflictBetweenLocalAndCloud.CloudTimeStamp;
                }
            }

            if (lastLocalTimestamp != DateTime.MinValue) {
                // If there is any conflict, ask user to resolve it
                resolution = await ResolveCloudConflict(lastLocalTimestamp, lastCloudTimestamp);
            }

            // Apply conflict resolution to all conflicts
            foreach (ConflictBetweenLocalAndCloud conflict in results.OfType<ConflictBetweenLocalAndCloud>().ToList()) {
                results.Remove(conflict);
                if (resolution == CloudConflictResolution.UseCloud) {
                    results.Add(conflict.ChooseCloud());
                } else {
                    results.Add(conflict.ChooseLocal());
                }
            }
            
            CloudService.Get.EndSaveBatch();
            
            // Save PrefMemory to ensure all sync changes will be applied to Origin
            PrefMemory.Save();
            
            // Failures will be handled later
            _cloudSyncFailures.AddRange(results.Where(r => r.Type == ResultType.Failure));
        }
        
        async UniTask<CloudConflictResolution> ResolveCloudConflict(DateTime localTimestamp, DateTime cloudTimestamp) {
            GameObject conflictResolutionPrefab = Resources.Load<GameObject>("Prefabs/CloudConflictResolution");
            var go = GameObject.Instantiate(conflictResolutionPrefab, uiInitializer.CanvasServices.MainTransform);
            CloudConflictUI conflictUI = go.GetComponent<CloudConflictUI>();

            CloudConflictResolution resolution = CloudConflictResolution.None;
            conflictUI.Init(localTimestamp, cloudTimestamp, () => resolution = CloudConflictResolution.UseLocal, () => resolution = CloudConflictResolution.UseCloud);

            await UniTask.WaitUntil(() => resolution != CloudConflictResolution.None);
            GameObject.Destroy(go);
            return resolution;
        }
        
        void HandleFailedCloudSync() {
            if (_cloudSyncFailures.Any()) {
                // Log to console
                Log.Marking?.Warning("Cloud Sync Failure");
                foreach (ICloudSyncResult result in _cloudSyncFailures) {
                    Log.Important?.Error(result.ToString());
                }
                
                // Send auto bug report
                // string summary = "CloudSyncFailure!";
                // string description = "Cloud Sync Failed, Report send automatically\n" +
                //                      string.Join("\n", _cloudSyncFailures.Select(f => f.ToString()));
                // AutoBugReporting.SendAutoReport(summary, description);
                
                _cloudSyncFailures.Clear();
            }
        }
        
        void InitServicesCrucialForCloudConflict() {
            World.AssignServices(new Services());
            // -- update provider
            Services.Register(UnityUpdateProvider.GetOrCreate());
            // -- social service
            var socialService = SocialService.CreateSocialService();
            if (socialService != null) {
                Services.Register(socialService);
            }
        }
        
        // === Initialize Services
        async UniTask InitializeServices() {
            // -- initialization, this service comes first because any of other can depends on this one
            Services.Register(_sceneInitialization);
            // -- execution
            Services.Register(servicesRefs.mitigatedExecution).Init();
            // -- culling
            Services.Register(new DistanceCullersService());
            
            // -- modding
            Services.Register(new PatcherService(gameConstants));
            Services.Register(new ModService());
            Services.Register(new TemplatesProvider());
            // -- scene service, for keeping track of all scenes
            var sceneService = Services.Register(new SceneService());
            await sceneService.InitAllSceneReferences();
            // -- large files tracking
            Services.Register(new LargeFilesStorage()).Init();
            // game constants: access to constant numbers that can be balanced from one place
            Services.Register(gameConstants);
            // references: access to constant templates & values from one place
            Services.Register(commonReferences);
            commonReferences.RegisterServices();
            Services.Register(debugReferences.Init());
            Services.Register(debugFishing.Init());
            Services.Register(servicesRefs.floatingText);
            Services.Register(servicesRefs.transitionService);
            Services.Register(actorsRegister);
            // tooltip storage
            Services.Register(new UITooltipStorage());
            Services.Register(servicesRefs.idleRefresher);
            Services.Register(servicesRefs.vSkillMachineParent);
            Services.Register(new CullingDistanceMultiplierService());

            // -- created
            //Services.Register(new RemoteConfig());
            // tweak system: modifies stats of models with serializable tweaks
            Services.Register(new TweakSystem());
            // Id Storage: responsible for assigning IDs to models
            Services.Register(new IdStorage()).Init();
            //Services.Register(patchNotesContainer);
            
            Services.Register(new FactionRegionsService());

            DOTween.SetTweensCapacity(500, 50);

            //Services.Register(remoteEventsService);
            
            Services.Register(servicesRefs.factionProvider);

            Services.Register(new DroppedItemSpawner()).Init();

            Services.Register(new LimitsGuardingService()).Init();
            Services.Register(new BarkSystem());
            Services.Register(new InteractionProvider());
            Services.Register(new NewThingsTracker()).Init();
            Services.Register(new CharacterLimitedLocations()).Init();

            Services.Register(postProcessService);
            Services.Register(new FpsLimiter());

            Services.Register(patchNotesContainer);
            
            Services.Register(new CombatDirector()).Init();
            Services.Register(new CircleAroundTargetService()).Init();
            Services.Register(new WyrdnessService()).Init(sceneService);
            DebugProjectNames.SyncDebugNamesCache();
        }
        
        // === Initialize World
        async UniTask InitializeWorld() {
            // -- Models that need to exist before loading
            World.Add(new SettingsMaster(await GraphicPresets.GetDefaultGraphicSetting()));
            World.Add(new CameraStateStack());
            World.Add(new GameCamera());
            World.Add(new UIStateStack());
            World.Add(new GameUI());
            World.Add(new ActionLog());
            // -- Load App level domains
            DomainUtils.LoadAppDomains();
            
            //Services.Get<RemoteConfig>().Init(true);
            //remoteEventsService.DataInit();
        }
        
        // === Prepare scene for new game state
        void InitializeWorldBasedServices() {
            // Cursor
            Services.Register(servicesRefs.cursor);
            servicesRefs.cursor.Initialize();

            // Global Keys: master key handler
            var globalKeys = Services.Register(new GlobalKeys());
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, globalKeys));
            
            Services.Register(new RecurringActions()).Init();
            Services.Register(new TimeQueue()).Init();

            Services.Register(autoAchievementsService);

            Services.Register(new DebugAI());
            Services.Get<AudioCore>().Initialize();
        }

        // === Game loop
        void LateUpdate() {
            if (!s_initCompleted) return;

            World.All<Story>().FillList(_allStoriesBuffer);
            foreach (var story in _allStoriesBuffer) {
                story?.ProcessEndFrame();
            }
            _allStoriesBuffer.Clear();
            
            Services.Get<Cursor>().ProcessEndFrame();
            World.VerifyAllInOrder();
        }

        [Button]
        void OnApplicationFocus(bool hasFocus) {
            if (!s_initCompleted) return;

#if UNITY_PS5 || UNITY_GAMECORE
            if (!World.HasAny<MenuUI>() && !World.HasAny<Cutscene>() && UIStateStack.Instance.State.IsMapInteractive) {
                World.Add(new MenuUI());
            }
#endif
            
            if (World.Any<DisableAudioInBackground>()?.Enabled ?? false) {
                float masterValue = World.All<Volume>().FirstOrDefault(v => v.Group == AudioGroup.MASTER)?.ModifiedValue ?? 1;
                AudioManager.SetAudioChannelVolume(AudioGroup.MASTER, hasFocus ? masterValue : 0);
            }
        }
        
        static void ClearScenesProcessorsCache() {
#if UNITY_EDITOR && !SCENES_PROCESSED
            try {
                var sceneProcessorTypeClass = Type.GetType(
                    "Awaken.Utility.Editor.Scenes.SceneProcessor, Awaken.Utility.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    true, true);
                var clearCacheMethod = sceneProcessorTypeClass.GetMethod("ResetAllScenesProcessedStatus", BindingFlags.Static | BindingFlags.Public);
                clearCacheMethod!.Invoke(null, null);

            } catch (Exception e) {
                Debug.LogException(e);
            }
#endif
        }

        void OnApplicationQuit() {
            if (!s_initCompleted) return;
            PrefMemory.Save();
            Services.TryGet<CullingSystem>()?.Discard();
        }

        void OnGUI() {
            if (!s_initCompleted) return;
            
            World.Any<GameUI>()?.PerformGUI();
        }

        void ApplicationQuitting() {
#if UNITY_EDITOR
            World.DropDomain(Domain.Globals);
#endif
            LogsCollector.Dispose();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Awaken.Utility.LowLevel.WindowsKernelHelpers.KillCurrentProcess();
#endif
        }
    }
}
