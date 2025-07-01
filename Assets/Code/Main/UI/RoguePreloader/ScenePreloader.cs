using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UniversalProfiling;
#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.RemoteEvents;
#endif

namespace Awaken.TG.Main.UI.RoguePreloader {
    public static class ScenePreloader {
        static ILoadingOperation s_loadingOperation;
        static IEventListener s_heroDamageEvent;
        public static bool IsLoadingCompleted => s_loadingOperation == null;

        public static void EDITOR_RuntimeReset() {
            s_loadingOperation = null;
            s_heroDamageEvent = null;
        }
        
        // === Execution
        [UnityEngine.Scripting.Preserve] public static void StartNewGame() => StartNewGame(null);
        public static void StartNewGame(SceneReference sceneReference) => LoadScene(new NewGameLoading(sceneReference));

        public static void Load(SaveSlot slot, string sourceInfo) {
            Log.Marking?.Warning($"Loading '{slot}' from '{sourceInfo}'");
            LoadScene(new FullLoading(slot));
            slot.LoadingStarted();
        }

        // TODO: Change Map should decide based on SceneConfig if this is Additive or not
        public static void ChangeMap(SceneReference newMap) => LoadScene(new MapChangeLoading(newMap));
        public static void LoadTitleScreen() => LoadScene(new TitleScreenLoading());
        public static void EditorLoad(SceneReference sceneReference) => EditorLoadScene(new MapChangeLoading(sceneReference));
        public static async UniTask ChangeMapAndWait(SceneReference scene, Event<LoadingScreenUI, LoadingScreenUI> @event, IModel owner) {
            if (World.Services.Get<SceneService>().ActiveSceneRef.Equals(scene)) {
                return;
            }
            bool teleporting = true;
            ChangeMap(scene);
            ModelUtils.DoForFirstModelOfType<LoadingScreenUI>(
                lsUI => lsUI.ListenTo(@event, _ => teleporting = false, owner),
                owner);
            await AsyncUtil.WaitWhile(owner, () => teleporting);
        }

        static void LoadScene(ILoadingOperation loadingOperation) {
            if (s_loadingOperation != null) {
                throw new Exception("Trying to load new scene when another loading in process");
            }

            s_loadingOperation = loadingOperation;
            Transition();
        }

        static void Transition() {
            StartPreventingDamage();
            var disableInput = new DisableInputHandler(null, LoadingScreenUI.BlockInputMillisecondDelay, 10);
            World.Only<GameUI>().AddElement(disableInput);

            Load(disableInput).Forget();
        }

        static async UniTaskVoid Load(DisableInputHandler disableInputHandler) {
            var additionalInfo = LoadingScreenUI.GetLoadingOperationAdditionalInfo(s_loadingOperation);
            var toCameraDuration = additionalInfo.UseFastTransition ? LoadingScreenUI.ToCameraDurationFast : LoadingScreenUI.ToCameraDuration;
            await World.Services.Get<TransitionService>().ToBlack(toCameraDuration);
            var loadSave = LoadSave.Get;
            while (!loadSave.CanCacheDomainForSceneChange()) {
                await UniTask.NextFrame();
            }
            SceneSwitchInform();

            UniversalProfiler.SetMarker(LoadSave.LoadSaveProfilerColor, "ScenePreloader.LoadStarted");

            var loadingScreen = World.Add(new LoadingScreenUI(s_loadingOperation, additionalInfo));
            loadingScreen.ListenTo(Model.Events.BeforeDiscarded, model => {
                if (disableInputHandler is { HasBeenDiscarded: false }) {
                    disableInputHandler.DelayedDiscard(model);
                }

                StopPreventingDamage();
                s_loadingOperation = null;

                UniversalProfiler.SetMarker(LoadSave.LoadSaveProfilerColor, "ScenePreloader.LoadFinished");
            });
        }

        static void SceneSwitchInform() {
#if !UNITY_GAMECORE && !UNITY_PS5
            World.Services?.TryGet<RemoteEventsService>()?.SceneSwitch();
#endif
        }

        static void EditorLoadScene(ILoadingOperation loadingOperation) {
            if (s_loadingOperation != null) {
                throw new Exception("Trying to load new scene when another loading in process");
            }

            s_loadingOperation = loadingOperation;
            EditorNoTransitionLock();
        }

        static void EditorNoTransitionLock() {
            var transition = World.Services.Get<TransitionService>();
            transition.SetToBlack();
            SceneSwitchInform();
            World.Add(new LoadingScreenUI(s_loadingOperation, LoadingScreenUI.GetLoadingOperationAdditionalInfo(s_loadingOperation), false));
            s_loadingOperation = null;
        }

        static void StartPreventingDamage() {
            if (Hero.Current != null) {
                s_heroDamageEvent = Hero.Current.HealthElement.ListenTo(HealthElement.Events.TakingDamage, PreventDamage);
            }
        }

        static void PreventDamage(HookResult<HealthElement, Damage> hook) {
            hook.Prevent();
        }

        public static void StopPreventingDamage() {
            if (s_heroDamageEvent != null) {
                World.EventSystem.RemoveListener(s_heroDamageEvent);
            }
        }
    }
}