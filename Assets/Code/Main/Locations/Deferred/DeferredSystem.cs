using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Unity.Collections;

namespace Awaken.TG.Main.Locations.Deferred {
    /// <summary>
    /// This name means that this system executes action that are not to be observed by the player,
    /// like replacing one 3d model with another.
    /// </summary>
    public partial class DeferredSystem : Model {
        public override ushort TypeForSerialization => SavedModels.DeferredSystem;

        public override Domain DefaultDomain => Domain.Gameplay;
        const string RecurringId = "Refresh";

        // === State
        [Saved] Dictionary<string, DeferredActionsBySceneData> _actionsByScenes = new();
        public bool OverrideDistanceConditions { get; private set; }

        SceneService SceneService { get; set; }
        
        // === Initialization
        protected override void OnInitialize() {
            SceneService = World.Services.Get<SceneService>();
            Services.Get<RecurringActions>().RegisterAction(Refresh, this, RecurringId, 3f, false);
            ModelUtils.ListenToFirstModelOfType(Hero.Events.HeroLongTeleported, DisableNextDistanceCondition, this);
            ModelUtils.ListenToFirstModelOfType(Hero.Events.AfterHeroRested, DisableNextDistanceCondition, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneFullyInitialized, this, DisableNextDistanceCondition);
        }

        protected override void OnRestore() {
            this.AfterFullyInitialized(() => {
                OnInitialize();
                DisableNextDistanceCondition();
            }, this);
        }

        // === API
        public void RegisterAction(DeferredAction action) {
            string key = action.SceneReference == null ? string.Empty : action.SceneReference.Name;
            if (!_actionsByScenes.TryGetValue(key, out var data)) {
                data = DeferredActionsBySceneData.Default;
                _actionsByScenes.Add(key, data);
            }

            data.actions.Add(action);
        }

        // === Private Logic
        void Refresh() {
            var mainScene = SceneService.MainSceneRef;
            RefreshActionsInScene(mainScene.Name);

            if (SceneService.AdditiveSceneRef != null) {
                RefreshActionsInScene(SceneService.AdditiveSceneRef.Name);
            }
            
            // === Refresh all actions that are not bound to any scene
            RefreshActionsInScene(string.Empty);
            OverrideDistanceConditions = false;

            return;
            
            void RefreshActionsInScene(string sceneName) {
                if (!_actionsByScenes.TryGetValue(sceneName, out var data)) return;
                data.cts?.Cancel();
                data.cts = null;
                
                bool repeatNextFrame = false;
                var actions = data.actions;
                for (int i = 0; i < actions.Count; i++) {
                    if (TryExecute(actions[i], ref repeatNextFrame)) {
                        actions.RemoveAt(i);
                        --i;
                    }
                }
                if (repeatNextFrame) {
                    data.cts = new CancellationTokenSource();
                    RefreshActionsInSceneNextFrame(sceneName, data.cts.Token).Forget();
                    return;
                }
                
                if (data.actions.Count == 0) {
                    _actionsByScenes.Remove(sceneName);
                }
            }

            async UniTaskVoid RefreshActionsInSceneNextFrame(string sceneName, CancellationToken token) {
                if (!await AsyncUtil.DelayFrame(this, 1, token)) {
                    return;
                }

                if (ValidScene(sceneName)) {
                    RefreshActionsInScene(sceneName);
                }

                bool ValidScene(string sceneName) {
                    if (sceneName == string.Empty) {
                        return true;
                    }
                    if (sceneName == SceneService.MainSceneRef.Name) {
                        return true;
                    }
                    if (SceneService.AdditiveSceneRef != null && sceneName == SceneService.AdditiveSceneRef.Name) {
                        return true;
                    }
                    return false;
                }
            }
        }

        static bool TryExecute(DeferredAction action, ref bool repeatNextFrame) {
            if (!action.ConditionsFulfilled()) {
                return false;
            }
            var result = action.TryExecute();
            if (result == Result.RepeatNextFrame) {
                repeatNextFrame = true;
                return false;
            }
            return result == Result.Success;
        }

        void DisableNextDistanceCondition() {
            OverrideDistanceConditions = true;
            Refresh();
        }

        // === Discard
        protected override void OnDiscard(bool fromDomainDrop) {
            Services.Get<RecurringActions>().UnregisterAction(this, RecurringId);
        }

        public enum Result {
            Success,
            RepeatNextFrame,
            Fail
        }
    }

    [Serializable]
    public partial class DeferredActionsBySceneData {
        public ushort TypeForSerialization => SavedTypes.DeferredActionsBySceneData;

        public CancellationTokenSource cts;
        [Saved] public List<DeferredAction> actions;

        public static DeferredActionsBySceneData Default => new DeferredActionsBySceneData() {
            cts = null,
            actions = new List<DeferredAction>()
        };
    }
}