using System;
using System.Collections;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable UseArrayEmptyMethod

namespace Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes {
    [DefaultExecutionOrder(MapScene.ExecutionOrder + 5)]
    public class AdditiveScene : MonoBehaviour, IMapScene, IListenerOwner {
        string AdditiveSceneInitID => $"{nameof(AdditiveScene)} init '{gameObject.scene.name}'";

        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference defaultGuard;
        
        [SerializeField, FoldoutGroup("Audio"), Header("Music")] public BaseAudioSource musicAudioSource;
        [SerializeReference, FoldoutGroup("Audio")] public IAudioSource[] musicAudioSources = new IAudioSource[0];
        [SerializeReference, FoldoutGroup("Audio")] public IAudioSource[] musicAlertAudioSources = new IAudioSource[0];
        [SerializeReference, FoldoutGroup("Audio")] public IAudioSource[] musicCombatAudioSources = new IAudioSource[0];
        [SerializeField, FoldoutGroup("Audio"), Header("Ambient")] public BaseAudioSource ambientAudioSource;
        [SerializeField, FoldoutGroup("Audio"), Header("Snapshot")] public BaseAudioSource snapshotAudioSource;
        bool _audioInitialized;

        public Func<bool> TryRestoreWorld { get; set; }
        public SceneInitializer SceneInitialization => World.Services.Get<SceneInitializer>();
        public bool InitializationCanceled { get; set; }
        public Scene[] UnityScenes => new[] { gameObject.scene };

        SceneReference SceneRef => SceneReference.ByScene(gameObject.scene);
        Services Services => World.Services;
        
        public bool TryGetDefaultGuard(out LocationTemplate template) => defaultGuard.TryGet(out template);

        void Awake() {
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.BeforeSceneValid, new SceneLifetimeEventData(false, SceneRef));
        }

        void Start() {
            SceneService.SceneLoaded(SceneRef);
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.OnSceneBecameValid, new SceneLifetimeEventData(false, SceneRef));

            InitAll().Forget();
        }

        async UniTaskVoid InitAll() {
            // Delay loading world
            await AsyncUtil.DelayFrameOrTime(this, 5, 100);
            await AsyncUtil.WaitUntil(gameObject, () => SceneLifetimeEvents.Get.ValidMainSceneState);
            var decalsCuller = gameObject.GetComponentInChildren<StaticDecalsCuller>();
            if (decalsCuller) {
                decalsCuller.InitSingular();
            }
            // Classic loading
            SceneInitialization.GetNewElement(AdditiveSceneInitID);
            SceneService.InitLoadBasedBehavioursOnScene(gameObject.scene);
            InitializeServices();
            bool success = InitializeWorld();
            if (!success) {
                if (!DomainErrorPopup.Displayed) {
                    FailAndReturnToTitleScreen().Forget();
                }
                return;
            }
            
            // Wait a few frames before ending the loading screen
            StartCoroutine(FinishInitializationAfterFewFrames());
            SceneInitialization.CompleteElement(AdditiveSceneInitID);
            
            // --- Audio
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, OnSceneChanged);
            InitializeAudio();
        }

        void InitializeServices() {
            SpecSpawner spawner = World.Services.Get<SpecSpawner>();
            spawner.Init(UnityScenes);
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterServicesInitialized, new SceneLifetimeEventData(false, SceneRef));
        }

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
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterWorldInitialized, new SceneLifetimeEventData(false, SceneRef));
            return true;
        }

        IEnumerator FinishInitializationAfterFewFrames() {
            var endOfFrame = new WaitForEndOfFrame();
            
            const int FrameDelay = 4;
            using (SceneInitialization.GetNewElement(FrameDelay + " frame delay")) {
                for (var i = 0; i < FrameDelay; i++) {
                    yield return endOfFrame;
                }
            }

            SceneInitialization.Clear();
            SceneService.SceneInitialized(SceneRef);
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, new SceneLifetimeEventData(false, SceneRef));
        }
        
        public async UniTaskVoid FailAndReturnToTitleScreen(string message = "") {
            SceneInitialization.CompleteElement(AdditiveSceneInitID);
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

        public ISceneLoadOperation Unload(bool isSameSceneReloading) {
            return SceneService.UnloadSceneAsync(SceneRef);
        }

        void OnDestroy() {
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.AfterSceneDiscarded, new SceneLifetimeEventData(false, SceneRef));
            World.EventSystem.RemoveAllListenersOwnedBy(this);
        }
        
        // === Audio
        void OnSceneChanged(SceneLifetimeEvents _) {
            if (Services.TryGet<SceneService>()?.ActiveSceneRef == SceneRef) {
                InitializeAudio();
            } else {
                SceneRootAudioUtils.UnloadSceneAudio(ref _audioInitialized, musicAudioSources,
                    musicAlertAudioSources, musicCombatAudioSources, ambientAudioSource, snapshotAudioSource);
            }
        }

        void InitializeAudio() {
            SceneRootAudioUtils.InitializeSceneAudio(ref _audioInitialized, true, musicAudioSources,
                musicAlertAudioSources, musicCombatAudioSources, ambientAudioSource, snapshotAudioSource);
        }
    }
}