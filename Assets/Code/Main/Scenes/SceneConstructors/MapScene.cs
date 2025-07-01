using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Locations.Regrowables;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
// ReSharper disable UseArrayEmptyMethod

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    /// <summary>
    /// The class responsible for setting up the map
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public class MapScene : MonoBehaviour, IMapScene, IListenerOwner {
        public const int ExecutionOrder = -10;
        string MapSceneInitializationID => $"{nameof(MapScene)} init '{gameObject.scene.name}'";

        // === Serialized references set in editor
        [SerializeField] GroundBounds ground;
        
        public StoryBookmark initialStory;
        
        [SerializeField, FoldoutGroup("Info Board")] VWaitForInputBoard waitForInputBoardView;
        [SerializeField, FoldoutGroup("Info Board")] bool displayBoardOneTime;
        [SerializeField, FoldoutGroup("Info Board"), ShowIf(nameof(displayBoardOneTime)), Tags(TagsCategory.Flag)]
        string boardFlag;
        [SerializeField, FoldoutGroup("Info Board"), ShowIf(nameof(displayBoardOneTime)), Tags(TagsCategory.Flag)]
        string[] flagsThatDisableInfoBoard = Array.Empty<string>();
        
        [SerializeField, FoldoutGroup("Systems")] protected AstarPath aStarPath;

        public AudioSceneSet audioSceneSet;
        
        [Header("Music")] 
        [SerializeReference, FoldoutGroup("Audio", VisibleIf = "@audioSceneSet == null"), HideReferenceObjectPicker, ListDrawerSettings(ListElementLabelName = "EDITOR_Label")] public BaseAudioSource[] musicAudioSources = Array.Empty<BaseAudioSource>();
        [SerializeReference, FoldoutGroup("Audio"), HideReferenceObjectPicker, ListDrawerSettings(ListElementLabelName = "EDITOR_Label")] public BaseAudioSource[] musicAlertAudioSources = new BaseAudioSource[0];
        [SerializeReference, FoldoutGroup("Audio"), HideReferenceObjectPicker, ListDrawerSettings(ListElementLabelName = "EDITOR_Label")] public CombatMusicAudioSource[] musicCombatAudioSources = Array.Empty<CombatMusicAudioSource>();
        [SerializeField, FoldoutGroup("Audio"), Header("Ambient")] public BaseAudioSource ambientAudioSource;
        [SerializeField, FoldoutGroup("Audio"), Header("Snapshot")] public BaseAudioSource snapshotAudioSource;

        [SerializeField, FoldoutGroup("Systems")] bool disableFallDamage;
        [SerializeField, FoldoutGroup("Systems")] LeshyManager leshy;
        bool _audioInitialized;

        FogOfWar _fogOfWar;
        
        public bool WasActiveScene { get; set; }
        bool FastStart => DebugReferences.FastStart || DebugReferences.ImmediateStory;
        public SceneInitializer SceneInitialization => World.Services.Get<SceneInitializer>();
        
        // === Fields & Properties
        public Func<bool> TryRestoreWorld { get; set; }
        public bool InitializationCanceled { get; set; }
        public bool IsInitialized { get; protected set; }
        public SceneReference SceneRef { get; private set; }
        public virtual Scene[] UnityScenes => new[] { gameObject.scene };

        protected Services Services => World.Services;
        protected bool IsRestored => TryRestoreWorld != null;

        // === Initialization
        UniTask _wait;
        void Awake() {
            _wait = ApplicationScene.WaitForAppInit();
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.BeforeSceneValid, new SceneLifetimeEventData(true, SceneRef));
        }

        void Start() {
            SceneRef = SceneReference.ByScene(gameObject.scene);
            SceneService.SceneLoaded(SceneRef);
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.OnSceneBecameValid, new SceneLifetimeEventData(true, SceneRef));

            InitAll().Forget();

            // TODO: Remove when Unity fixes Foam on consoles
            if (PlatformUtils.IsConsole) {
                foreach (var waterSurface in FindObjectsByType<WaterSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    waterSurface.foam = false;
                }
            }
        }

        protected virtual async UniTaskVoid InitAll() {
            StaticDecalsCuller.MergeSubscenesCullersIntoOwnerSceneCullerAndEnable();
            // Delay loading world
            await AsyncUtil.DelayFrameOrTime(this, 5, 100);
            // First check if everything is fine
            CheckReferences();
            // Wait for app loading (only editor) 
            await _wait;
            
            if (InitializationCanceled) {
                FailAndReturnToTitleScreen().Forget();
                return;
            }
            
            // Init services (gameplay might need them)
            InitializeServices();
            // Init gameplay if absent
            if (!World.HasAny<Hero>()) {
                GameplayConstructor.CreateGameplay();
            }
            // Start proper map initialization
            SceneInitialization.GetNewElement(MapSceneInitializationID);
            SceneService.InitLoadBasedBehaviours();
            
            // It has to be done here - after Creating gameplay and before Initializing world
            Services.Register(new RegrowableService()).Init(World.Only<GameRealTime>());
            Services.Register(new PickableService());
            
            if (CommonReferences.Get.MapData.byScene.TryGetValue(SceneRef, out var mapData)) {
                var mapService = Services.Get<MapService>();
                mapService.Visit(SceneRef);
                if (mapData.HasFogOfWar) {
                    _fogOfWar = mapService.LoadFogOfWar(SceneRef);
                }
            }

            bool success = InitializeWorld();
            if (!success) {
                FailAndReturnToTitleScreen().Forget();
                return;
            }
            
            InitializeMapAttachments();
            if (_fogOfWar != null) {
                await UniTask.WaitUntil(_fogOfWar.IsInitialized);
            }
            // Wait a few frames before ending the loading screen
            StartCoroutine(FinishInitializationAfterFewFrames(SceneRef));
            
            SceneInitialization.CompleteElement(MapSceneInitializationID);
            IsInitialized = true;
            
            // --- Audio
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, OnSceneChanged);
            InitializeAudio();
            
            // --- FallDamage
            if (disableFallDamage) {
                Hero.Current?.TryGetElement<HeroFallDamage>()?.SetFallDamageEnabled(false);
            }
            // [HACK] We need dummySkillCharacter to exist before AI start using World.All<ICharacter>
            _ = DummySkillCharacter.GetOrCreateInstance;
        }

        [Conditional("DEBUG")]
        void CheckReferences() {
            var unassignedFields = new List<FieldInfo>();
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

        IEnumerator FinishInitializationAfterFewFrames(SceneReference sceneRef) {
            var endOfFrame = new WaitForEndOfFrame();
            
            const int FrameDelay = 4;
            using (SceneInitialization.GetNewElement(FrameDelay + " frame delay")) {
                for (var i = 0; i < FrameDelay; i++) {
                    yield return endOfFrame;
                }
            }

            SceneInitialization.Clear();
            SceneService.SceneInitialized(sceneRef);
            
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, new SceneLifetimeEventData(true, sceneRef));
            if (World.Any<LoadingScreenUI>() is {IsFPSStable: false} ls) {
                ls.ListenTo(LoadingScreenUI.Events.FpsBecameStable, _ => StartInitStories().Forget(), this);
            } else {
                StartInitStories().Forget();
            }
        }

        async UniTaskVoid StartInitStories() {
            if (!FastStart) {
                await TryToSpawnInitialBoard();
                await TitleScreenUtils.RunFirstSettings();
                await TitleScreenUtils.RunHeroCharacterCreatorIfNeeded(this);
            } else {
                TitleScreenUtils.FastHeroCharacterCreatorIfNeeded();
            }

            if (InitializationCanceled) {
                CleanupInitialization();
                return;
            }

            if (initialStory != null && (initialStory.story?.IsSet ?? false)) {
                StoryConfig config = StoryConfig.Base(initialStory, typeof(VDialogue));
                var story = Story.StartStory(config);
                await AsyncUtil.WaitForDiscard(story);
            }
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterSceneStoriesExecuted, new SceneLifetimeEventData(true, SceneRef));
        }
        
        async UniTask TryToSpawnInitialBoard() {
            if (waitForInputBoardView == null) {
                return;
            }
            
            if (displayBoardOneTime && Services.Get<GameplayMemory>().Context().Get<bool>(boardFlag)) {
                return;
            }
            
            if (flagsThatDisableInfoBoard.Any(flag => !flag.IsNullOrWhitespace() && Services.Get<GameplayMemory>().Context().Get<bool>(flag))) {
                return;
            }

            var waitForInputBoard = new WaitForInputBoard();
            World.Add(waitForInputBoard);
            World.SpawnViewFromPrefab<VWaitForInputBoard>(waitForInputBoard, waitForInputBoardView.gameObject);
            Services.Get<GameplayMemory>().Context().Set(boardFlag, true);

            await AsyncUtil.WaitForDiscard(waitForInputBoard);
        }

        public async UniTaskVoid FailAndReturnToTitleScreen(string message = "") {
            SceneInitialization.CompleteElement(MapSceneInitializationID);
            SceneInitialization.Clear();
            SceneService.SceneInitialized(SceneRef);
            if (TitleScreen.wasLoadingFailed == LoadingFailed.False) {
                TitleScreen.wasLoadingFailed = LoadingFailed.SaveFile;
            }
            TitleScreen.loadingFailedMessage = $"[{TitleScreen.wasLoadingFailed.ToStringFast()}] {message}";
            if (!DomainErrorPopup.Displayed) {
                await UniTask.WaitWhile(World.HasAny<LoadingScreenUI>);
                ScenePreloader.LoadTitleScreen();
            }
        }
        
        void CleanupInitialization() {
            SceneInitialization.CompleteElement(MapSceneInitializationID);
            SceneInitialization.Clear();
            SceneService.SceneInitialized(SceneRef);
        }

        // === Initialize Services
        void InitializeServices() {
            if (ground != null) {
                Services.Register(ground);
            }

            CullingSystem.Init(); // Will register self to services automatically

            Services.Register(new SpecSpawner()).Init(UnityScenes);
            
            if (leshy != null) {
                Services.Register(leshy);
            }

            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterServicesInitialized, new SceneLifetimeEventData(true, SceneRef));
        }

        void InitializeMapAttachments() {
            GetComponentsInChildren<IMapAttachment>().ForEach(x => x.Init());
        }

        // === Initialize World
        bool InitializeWorld() {
            bool restoring = false;
            if (TryRestoreWorld != null) {
                try {
                    restoring = TryRestoreWorld();
                    TryRestoreWorld = null;
                } catch (Exception e) {
                    Log.Important?.Error("Save file corrupted! Exception below");
                    Debug.LogException(e);
                    if (TitleScreen.wasLoadingFailed == LoadingFailed.False) {
                        TitleScreen.wasLoadingFailed = LoadingFailed.SaveFile;
                    }
                    return false;
                }
            }

            if (restoring) {
                Services.Get<SpecSpawner>().RestoreSpecs();
            } else {
                Services.Get<SpecSpawner>().SpawnAllSpecs();
            }
            
            Services.Get<SpecSpawner>().Clear();
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterWorldInitialized, new SceneLifetimeEventData(true, SceneRef));
            return true;
        }

        // === Game loop
        void FixedUpdate() {
            if (!IsInitialized) {
                return;
            }
            World.Only<GameRealTime>().ProcessFixedUpdate();
        }

        void Update() {
            if (!IsInitialized) {
                return;
            }
            World.Only<GameRealTime>().ProcessUpdate();
        }

        void LateUpdate() {
            if (!IsInitialized) {
                return;
            }
            World.Only<GameRealTime>().ProcessLateUpdate();

            Hero.Current.CheckForDeath();
        }
        
        // === Audio
        void OnSceneChanged(SceneLifetimeEvents _) {
            if (Services.TryGet<SceneService>()?.ActiveSceneRef == SceneRef) {
                InitializeAudio();
            } else {
                if (audioSceneSet != null) {
                    SceneRootAudioUtils.UnloadSceneAudio(ref _audioInitialized, audioSceneSet.musicAudioSources,
                        audioSceneSet.musicAlertAudioSources, audioSceneSet.musicCombatAudioSources, audioSceneSet.ambientAudioSource, audioSceneSet.snapshotAudioSource);
                    return;
                }
                SceneRootAudioUtils.UnloadSceneAudio(ref _audioInitialized, musicAudioSources,
                    musicAlertAudioSources, musicCombatAudioSources, ambientAudioSource, snapshotAudioSource);
            }
        }

        void InitializeAudio() {
            if (audioSceneSet != null) {
                SceneRootAudioUtils.InitializeSceneAudio(ref _audioInitialized, audioSceneSet.interpolateCombatLevel, audioSceneSet.musicAudioSources,
                    audioSceneSet.musicAlertAudioSources, audioSceneSet.musicCombatAudioSources, audioSceneSet.ambientAudioSource, audioSceneSet.snapshotAudioSource);
                return;
            }

            SceneRootAudioUtils.InitializeSceneAudio(ref _audioInitialized, true, musicAudioSources,
                musicAlertAudioSources, musicCombatAudioSources, ambientAudioSource, snapshotAudioSource);
        }
        
        // === Unload
        public virtual ISceneLoadOperation Unload(bool isSameSceneReloading) {
            IsInitialized = false;
            return SceneService.UnloadSceneAsync(SceneRef);
        }

        public void RemoveMainPathfinding() {
            if (aStarPath == null) {
                Log.Important?.Error("AstarPath is not assigned", gameObject);
                return;
            }
            
            if (AstarPath.active != aStarPath) {
                return;
            }
            
            AIBase.PauseAll();
            aStarPath.FastDisable();
            AstarPath.active = null;
        }

        public void RestoreMainPathfinding() {
            if (aStarPath == null) {
                Log.Important?.Error("AstarPath is not assigned", gameObject);
                return;
            }
            
            AstarPath.active = aStarPath;
            aStarPath.FastEnable();
        }

        // === Cleanup
        void OnDestroy() {
            if (_fogOfWar != null) {
                World.Services.TryGet<MapService>()?.ReleaseFogOfWar(_fogOfWar);
            }
            if (disableFallDamage) {
                Hero.Current?.TryGetElement<HeroFallDamage>()?.SetFallDamageEnabled(true);
            }

            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterSceneDiscarded, new SceneLifetimeEventData(true, SceneRef));
            World.EventSystem.RemoveAllListenersOwnedBy(this);
        }
        
        // === Editor Helpers

#if UNITY_EDITOR
        [Button, FoldoutGroup("Audio")]
        void MigrateAudioToSceneSet() {
            audioSceneSet = AudioSceneSet.CreateFrom(this);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
