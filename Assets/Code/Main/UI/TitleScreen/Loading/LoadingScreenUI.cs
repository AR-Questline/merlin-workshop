using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.TG.Assets;
using Awaken.TG.Debugging;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Animations;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI.TitleScreen.Loading {
    [SpawnsView(typeof(VLoadingScreenUI))]
    public partial class LoadingScreenUI : Model, IUIStateSource {
        public const float ToCameraDuration = TransitionService.DefaultFadeIn;
        public const float ToCameraDurationFast = TransitionService.DefaultFadeOut;
        public const int BlockInputMillisecondDelay = (int)((ToCameraDelay + ToCameraDuration) * 1.1f * 1000);
        const float ToCameraDelay = 0.1f;
        const float StuckMaxTime = 5;
        const int DrakeLoadingMaxWaitFramesCount = 20;

        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.BlockInput.WithPauseTime();

        public ILoadingOperation LoadingOperation { get; }
        bool ShouldUnloadUnused { get; }
        public bool UseFastTransition { get; }
        public SceneInitializationHandle SceneInitializationHandle { get; private set; }
        public SceneReference PreviousScene { get; private set; }
        public bool IsFPSStable { get; private set; }
        public static bool IsLoading { get; private set; }
        public static bool IsFullyLoadingOrCreatingNewGame => World.Any<LoadingScreenUI>()?.LoadingType is LoadingType.Full or LoadingType.NewGame;
        public static bool IsFullyLoading => World.Any<LoadingScreenUI>()?.LoadingType is LoadingType.Full;
        public LoadingType LoadingType => LoadingOperation?.Type ?? LoadingType.None;

        IMapScene _mapScene;

        SceneService SceneService => World.Services.Get<SceneService>();

        public new static class Events {
            public static readonly Event<LoadingScreenUI, LoadingScreenUI> SceneInitializationStarted = new(nameof(SceneInitializationStarted));
            public static readonly Event<LoadingScreenUI, LoadingScreenUI> InitializationElementsChanged = new(nameof(InitializationElementsChanged));
            public static readonly Event<LoadingScreenUI, LoadingScreenUI> SceneInitializationEnded = new(nameof(SceneInitializationEnded));
            public static readonly Event<LoadingScreenUI, LoadingScreenUI> BeforeDroppedPreviousDomain = new(nameof(BeforeDroppedPreviousDomain));
            public static readonly Event<LoadingScreenUI, LoadingScreenUI> AfterDroppedPreviousDomain = new(nameof(AfterDroppedPreviousDomain));
            public static readonly Event<LoadingScreenUI, LoadingScreenUI> FpsBecameStable = new(nameof(FpsBecameStable));
        }

        public LoadingScreenUI(ILoadingOperation loadingOperation, LoadingOperationAdditionalInfo additionalInfo, bool? shouldUnloadUnused = null) {
            bool useFastTransition = additionalInfo.UseFastTransition;
            LoadingOperation = loadingOperation;
            // Temporary disable to check if improve xbox situation
            ShouldUnloadUnused = shouldUnloadUnused ?? true; //!additionalInfo.isLoadingInterior;
            UseFastTransition = useFastTransition;

            if (additionalInfo.isLoadingInterior && !additionalInfo.isUnloadingInterior) {
                OnEnteringInteriorFirstTime();
            }
        }

        protected override void OnInitialize() {
            LoadingStates.IsLoadingWorld = true;
            World.Services.Get<FpsLimiter>().RegisterLimit(this, 60);
            Application.backgroundLoadingPriority = ThreadPriority.High;
            Texture.allowThreadedTextureCreation = false;
        }

        public void InitHeavy() {
            Services.Get<TemplatesProvider>().StartLoading(true);
            Services.TryGet<AudioCore>()?.Initialize();
            PreviousScene = SceneService.ActiveSceneRef;
        }

        public void DropPreviousDomains() {
            LoadingOperation.DropPreviousDomains(PreviousScene);
        }

        public IEnumerable<ISceneLoadOperation> UnloadPrevious() {
            IsLoading = true;
            bool isSameSceneReloading = LoadingOperation.SceneToLoad?.Equals(PreviousScene) ?? false;
            foreach (var sceneToUnload in LoadingOperation.ScenesToUnload(PreviousScene)) {
                if (sceneToUnload != null && sceneToUnload.LoadedScene.isLoaded) {
                    ISceneLoadOperation operation = sceneToUnload.RetrieveSceneForUnloading().Unload(isSameSceneReloading);
                    yield return operation;
                }
            }
        }

        public ISceneLoadOperation Load() {
            // Previous scene has been unloaded, new one will be loaded soon, it's time to clean some caches
            DOTween.Clear();
            AnimatorUtils.ResetCache();
            if (ShouldUnloadUnused) {
                MemoryClear.ClearProgramming().Forget();
            }
            World.Only<SettingsMaster>().PerformOnSceneChange();
            return LoadingOperation.Load(this);
        }

        public void NewSceneLoaded(SceneReference scene) {
            if (PreviousScene is { IsAdditive: true } && scene.IsAdditive == false) {
                AIBase.UnpauseAll();    
            }
            
            RegisterNewScene(scene);
            WaitForSceneInitialization();
            if (_mapScene != null) {
                SceneInitializationHandle.OnInitialized += CleanupAfterLoading;
            }
        }

        public void RegisterNewScene(SceneReference scene) {
            // Set new scene as active
            var loadedScene = scene.LoadedScene;
            SceneManager.SetActiveScene(loadedScene);

            // Retrieve scene behaviour
            _mapScene = scene.RetrieveMapScene();
            SceneService.ChangeTo(scene, isAdditive: _mapScene is AdditiveScene);

            // Perform loading-related operations on loaded scene
            LoadingOperation.OnComplete(_mapScene);
        }

        public void WaitForSceneInitialization() {
            // Listen to scene initialization end
            if (_mapScene != null) {
                this.Trigger(Events.SceneInitializationStarted, this);
                SceneInitializationHandle = _mapScene.SceneInitialization.SceneInitializationHandle;
                SceneInitializationHandle.OnRemainedElementsChanged += SceneInitializationProgressed;
            } else {
                OnTitleScreenLoaded();
            }
        }

        public void OnNothingLoaded() {
            this.Trigger(Events.SceneInitializationStarted, this);
            CleanupAfterLoading();
        }

        public static LoadingOperationAdditionalInfo GetLoadingOperationAdditionalInfo(ILoadingOperation loadingOperation) {
            var currentScene = World.Services.Get<SceneService>().ActiveSceneRef;
            var isUnloadingInterior = currentScene != null && currentScene.IsAdditive;
            var sceneToLoadRef = loadingOperation.SceneToLoad;
            var isLoadingInterior = sceneToLoadRef.IsAdditive;
            bool isLoadingNotPreloadedScene = (sceneToLoadRef.LoadedScene.IsValid() == false) ||
                                                 (sceneToLoadRef.RetrieveMapScene() is MapScene { WasActiveScene: false } || 
                                                  loadingOperation.Type is LoadingType.Full or LoadingType.NewGame);

            return new LoadingOperationAdditionalInfo(isUnloadingInterior, isLoadingInterior, isLoadingNotPreloadedScene);
        }

        public void DisableLeshy() {
            var leshyManagers = Object.FindObjectsByType<LeshyManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var managersCount = leshyManagers.Length;
            foreach (var sceneToUnload in LoadingOperation.ScenesToUnload(PreviousScene)) {
                if (sceneToUnload != null && sceneToUnload.LoadedScene.isLoaded) {
                    for (int i = managersCount - 1; i >= 0; i--) {
                        if (leshyManagers[i].gameObject.scene == sceneToUnload.LoadedScene) {
                            leshyManagers[i].gameObject.SetActive(false);
                            leshyManagers[i] = leshyManagers[managersCount - 1];
                            managersCount--;
                        }
                    }
                }
            }
        }

        void OnTitleScreenLoaded() {
            ClearReferences();
            LoadingOperation.Dispose();
            Discard();
            World.Services.Get<TransitionService>().ToCamera(ToCameraDurationFast).Forget();
            Log.Marking?.Warning("Loading: Returned to TitleScreen");
        }

        void SceneInitializationProgressed() {
            this.Trigger(Events.InitializationElementsChanged, this);
        }

        public void CleanupAfterLoading() {
            AsyncCleanupAfterLoading().Forget();
        }

        public async UniTaskVoid AsyncCleanupAfterLoading() {
            this.Trigger(Events.SceneInitializationEnded, this);
            ClearReferences();
            Log.Marking?.Warning("Loading: Finalizing");
            await EndLoadingTransition();
            LoadingOperation.Dispose();
            Log.Marking?.Warning("Loading: Completed");
            RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.Medium);
            Discard();
        }

        void ClearReferences() {
            if (SceneInitializationHandle != null) {
                SceneInitializationHandle.OnInitialized -= CleanupAfterLoading;
                SceneInitializationHandle.OnRemainedElementsChanged -= SceneInitializationProgressed;
            }

            MemoryClear.ReferencesCachesRevalidate();
            SceneInitializationHandle = null;
            IsLoading = false;
        }

        async UniTask EndLoadingTransition() {
            // --- Delays
            await UniTask.WaitUntil(() => SceneLifetimeEvents.Get.EverythingInitialized || TitleScreen.wasLoadingFailed != LoadingFailed.False);

            await AsyncUtil.DelayFrame(this);

            LoadingStates.IsLoadingWorld = false;
            var loadingStartTime = Time.unscaledTime;
            var loadingHash = LoadingHash();

            while (LoadingStates.LoadingLocations > 0 | LoadingStates.IsLoadingHlods) {
                await AsyncUtil.DelayFrame(this, 2);

                var currentHash = LoadingHash();
                if (loadingHash == currentHash) {
                    if (Time.unscaledTime - loadingStartTime > StuckMaxTime) {
                        Log.Critical?.Error("Loading: stuck, breaking loop");
                        LoadingStates.LoadingLocations = 0;
                        LoadingStates.IsLoadingHlods = false;
                        break;
                    }
                } else {
                    loadingHash = currentHash;
                    loadingStartTime = Time.unscaledTime;
                }
            }

            var drakeRendererLoadingSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<DrakeRendererLoadingSystem>();
            int drakeDelayCounter = 0;
            if (drakeRendererLoadingSystem != null) {
                while (drakeRendererLoadingSystem.IsLoadingAnyEntities && drakeDelayCounter < DrakeLoadingMaxWaitFramesCount) {
                    drakeDelayCounter++;
                    await AsyncUtil.DelayFrame(this);
                }
            }

            if (ShouldUnloadUnused) {
                await MemoryClear.ClearProgramming();
            }

            bool isFinishingLoadingNotInterior = LoadingOperation.SceneToLoad == null ||
                                                 LoadingOperation.SceneToLoad.IsAdditive == false;
            if (isFinishingLoadingNotInterior) {
                OnExitingInterior();
            }

            var transition = World.Services.Get<TransitionService>();

            if (!UseFastTransition) {
                await SmoothFPS(this);
                // --- To Black, Loading screen has completed all work
                await transition.ToBlack(ToCameraDuration);
            }

            Log.Marking?.Warning("Loading: fps stable");

            IsFPSStable = true;
            this.Trigger(Events.FpsBecameStable, this); // Starts scene story

            // Should we continue handling transition or will something else handle them
            if (World.Any<Video>()?.IsFullScreen ?? false) {
                return;
            }

            // Should we continue handling transition or will something else handle them (secondary checks)
            if (World.HasAny<Cutscene>()) {
                return;
            }

            // --- Smoothen ToCamera
            if (UseFastTransition) {
                await SmoothFPSFast(this);
            } else {
                await SmoothFPS(this);
            }

            if (LoadingOperation.SceneToLoad?.RetrieveMapScene() is MapScene mapScene) {
                mapScene.WasActiveScene = true;
            }

            if (UseFastTransition) {
                transition.ToCamera(ToCameraDurationFast).Forget();
            } else {
                transition.ToCamera(ToCameraDuration, ToCameraDelay).Forget();
            }

            int LoadingHash() {
                unchecked {
                    var hash = (int)LoadingStates.LoadingLocations;
                    hash = (hash * 397) ^ LoadingStates.IsLoadingHlods.GetHashCode();
                    return hash;
                }
            }
        }

        public static async UniTask SmoothFPS(IModel model) {
            var fpsChecker = new SmoothFpsChecker(120, 0.15f);
            while (fpsChecker.FpsAreUnstable()) {
                await AsyncUtil.DelayFrame(model);
            }
        }

        public static async UniTask SmoothFPSFast(IModel model) {
            var fpsChecker = new SmoothFpsChecker(10, 0.7f);
            while (fpsChecker.FpsAreUnstable()) {
                await AsyncUtil.DelayFrame(model);
            }
        }

        protected override void OnFullyDiscarded() {
            // if another loading screen was started. do not overwrite
            if (World.HasAny<LoadingScreenUI>()) return;
            Log.Marking?.Warning("Loading: Fully discarded");
            Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
            Texture.allowThreadedTextureCreation = true;
        }

        static void OnExitingInterior() {
            if (World.Services.TryGet(out CullingSystem cullingSystem)) {
                cullingSystem.UnpauseAllCullingGroupsElements();
            }

            LoadingStates.PauseHlodUpdateByInterior = false;
            Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<DrakeRendererStateSystem>().UnfreezeInstancesLOD();
        }

        static void OnEnteringInteriorFirstTime() {
            if (World.Services.TryGet(out CullingSystem cullingSystem)) {
                cullingSystem.PauseCurrentElementsDistanceBands();
            }

            LoadingStates.PauseHlodUpdateByInterior = true;
            Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<DrakeRendererStateSystem>().FreezeLoadedInstancesLOD();
        }

        public struct LoadingOperationAdditionalInfo {
            public bool isUnloadingInterior;
            public bool isLoadingInterior;
            public bool isLoadingNotPreloadedScene;
            public bool UseFastTransition => (isLoadingInterior || (isUnloadingInterior && !isLoadingNotPreloadedScene));

            public LoadingOperationAdditionalInfo(bool isUnloadingInterior, bool isLoadingInterior, bool isLoadingNotPreloadedScene) {
                this.isUnloadingInterior = isUnloadingInterior;
                this.isLoadingInterior = isLoadingInterior;
                this.isLoadingNotPreloadedScene = isLoadingNotPreloadedScene;
            }
        }
    }
}