using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes;
using Awaken.TG.Main.Utility.Patchers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [DefaultExecutionOrder(0)]
    public class TitleScreen : MonoBehaviour, IDomainBoundService, IScene {
        [SerializeField] SceneReference[] sceneReferences = Array.Empty<SceneReference>();
        [SerializeField] [UnityEngine.Scripting.Preserve] HeroPreset[] heroPresets = Array.Empty<HeroPreset>();
        
        public Domain Domain => Domain.TitleScreen;
        public bool RemoveOnDomainChange() => true;

        public static LoadingFailed wasLoadingFailed = LoadingFailed.False;
        public static string loadingFailedMessage = string.Empty;
        public static bool autoContinueGame = false;

        public static void EDITOR_RuntimeReset() {
            wasLoadingFailed = LoadingFailed.False;
        }
        
        // === Unity Lifecycle
        void Start() {
            SceneService.SceneLoaded(TitleScreenLoading.TitleScreenRef);
            InitScene().Forget();
        }

        async UniTaskVoid InitScene() {
#if UNITY_EDITOR
            Configuration.InitializeData();
#endif
            await ApplicationScene.WaitForAppInit();
            World.Services.Register(this);
            World.Services.Get<SceneService>().ChangeTo(SceneReference.ByScene(gameObject.scene));
            var titleScreenUI = World.Add(new TitleScreenUI());
            titleScreenUI.AddElement<TimeDependentsCache>();
            titleScreenUI.AddElement(new TitleScreenSceneSelection(sceneReferences));
            World.Services.Get<IdleBehavioursRefresher>().Cleanup();
            SceneService.SceneInitialized(TitleScreenLoading.TitleScreenRef);

            if (wasLoadingFailed != LoadingFailed.False) {
                try {
                    HandleFailedLoading();
                } catch (Exception e) {
                    Debug.LogException(e);
                } finally {
                    wasLoadingFailed = LoadingFailed.False;
                    loadingFailedMessage = string.Empty;
                }
            }
        }

        void HandleFailedLoading() {
            if (wasLoadingFailed != LoadingFailed.False && wasLoadingFailed != LoadingFailed.CachedDomain) {
                PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.LoadingFailedSaveFile.Translate());
            }

            string summary = $"CRITICAL! Reason {wasLoadingFailed} {loadingFailedMessage}";
            string description = "Failed to load game, Report sent automatically";
            AutoBugReporting.SendAutoReport(summary, description);
        }

        // === Run TimeDependents
        void FixedUpdate() {
            var cache = World.Any<TimeDependentsCache>();
            if (cache) {
                GameRealTime.RunTimeDependentModelsFixedUpdate(cache);
            }
        }
        void Update() {
            var cache = World.Any<TimeDependentsCache>();
            if (cache) {
                GameRealTime.RunTimeDependentModelsUpdate(cache);
            }
        }
        void LateUpdate() {
            var cache = World.Any<TimeDependentsCache>();
            if (cache) {
                GameRealTime.RunTimeDependentModelsLateUpdate(cache);
            }
        }

        public ISceneLoadOperation Unload(bool isSameSceneReloading) {
            return SceneService.UnloadSceneAsync(SceneReference.ByScene(gameObject.scene));
        }
    }

    public enum LoadingFailed : byte {
        False = 0,
        SaveFile = 1,
        ModelInitialization = 2,
        CachedDomain = 3,
    }

    [Serializable]
    public class HeroPreset {
        [UnityEngine.Scripting.Preserve] public string name;
        [TemplateType(typeof(HeroTemplate))] [UnityEngine.Scripting.Preserve]
        public TemplateReference heroRef;
    }
}
