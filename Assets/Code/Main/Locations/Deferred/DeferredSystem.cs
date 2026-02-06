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
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        public DeferredActionsBySceneData ActionsByScene(string sceneName) => _actionsByScenes.GetValueOrDefault(sceneName);
        public IEnumerable<DeferredActionsBySceneData> AllActionsByScenes => _actionsByScenes.Values;
        
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

        public void TryRefreshAction(DeferredAction action) {
            string key = action.SceneReference == null ? string.Empty : action.SceneReference.Name;
            if (key == string.Empty || key == SceneService.MainSceneRef.Name || key == SceneService.AdditiveSceneRef?.Name) {
                if (_actionsByScenes.TryGetValue(key, out var data) && data.cts == null) {
                    RefreshActionInSceneNextFrame(key, data).Forget();
                }
            }
        }

        // === API
        public void RegisterAction(DeferredAction action) {
            string key = action.SceneReference == null ? string.Empty : action.SceneReference.Name;
            if (key == null) {
                Log.Critical?.Error($"Deferred action has a scene reference to {action.SceneReference.Domain.FullName} with no name: {action.SceneReference.GetDebugInfo()}");
                key = string.Empty;
            }
            if (!_actionsByScenes.TryGetValue(key, out var data)) {
                data = DeferredActionsBySceneData.Default;
                _actionsByScenes.Add(key, data);
            }

            data.AddAction(action);
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
        }

        void RefreshActionsInScene(string sceneName) {
            if (!_actionsByScenes.TryGetValue(sceneName, out var data)) return;
            RefreshActionsInScene(sceneName, data);
        }
        
        void RefreshActionsInScene(string sceneName, DeferredActionsBySceneData data) {
            data.cts?.Cancel();
            data.cts = null;

            bool anySuccess = false;
            foreach (var list in data.RuntimeActions) {
                if (!TryExecute(list[0])) {
                    continue;
                }
                data.RemoveAction(list[0]);
                list.RemoveAt(0);
                anySuccess = true;

                for (int i = 0; i < list.Count; i++) {
                    if (TryExecute(list[i])) {
                        data.RemoveAction(list[i]);
                        list.RemoveAt(i);
                        --i;
                    }
                }
            }

            if (!anySuccess) {
                return;
            }

            for (int i = 0; i < data.RuntimeActions.Count; i++) {
                if (data.RuntimeActions[i].IsEmpty()) {
                    data.RuntimeActions.RemoveAt(i);
                    --i;
                }
            }
            
            if (data.IsEmpty) {
                _actionsByScenes.Remove(sceneName);
            }
        }
        
        async UniTaskVoid RefreshActionInSceneNextFrame(string sceneName, DeferredActionsBySceneData data) {
            data.cts = new CancellationTokenSource();
            if (!await AsyncUtil.DelayFrame(this, 1, data.cts.Token)) {
                return;
            }
            RefreshActionsInScene(sceneName, data);
        }

        static bool TryExecute(DeferredAction action) {
            if (!action.ConditionsFulfilled()) {
                return false;
            }
            var result = action.TryExecute();
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
            Ignore,
            Fail
        }
    }

    [Serializable]
    public partial class DeferredActionsBySceneData {
        public ushort TypeForSerialization => SavedTypes.DeferredActionsBySceneData;

        public CancellationTokenSource cts;
        [Saved] public List<DeferredAction> actions;
        List<List<DeferredAction>> _runtimeActions;
        
        public List<List<DeferredAction>> RuntimeActions => _runtimeActions ??= CreateRuntimeActions();
        
        public bool IsEmpty => actions.Count == 0;

        public static DeferredActionsBySceneData Default => new DeferredActionsBySceneData() {
            cts = null,
            actions = new List<DeferredAction>()
        };

        public void AddAction(DeferredAction action) {
            actions.Add(action);
            foreach (var list in RuntimeActions) {
                if (list[0].HasSimilarConditions(action)) {
                    list.Add(action);
#if AR_DEBUG || UNITY_EDITOR
                    if (list.Count > 4) {
                        DebugLogSuspiciousAction(list.Count, action);
                    }
#endif
                    return;
                }
            }
            RuntimeActions.Add(new List<DeferredAction>() {
                action
            });
        }

        public void RemoveAction(DeferredAction action) {
            actions.Remove(action);
        }

        List<List<DeferredAction>> CreateRuntimeActions() {
            var optimizedActions = new List<List<DeferredAction>>();
            foreach (var action in actions) {
                bool found = false;
                foreach (var list in optimizedActions) {
                    if (action.HasSimilarConditions(list[0])) {
                        list.Add(action);
                        found = true;
#if AR_DEBUG || UNITY_EDITOR
                        if (list.Count > 4) {
                            DebugLogSuspiciousAction(list.Count, action);
                        }
#endif
                        break;
                    }
                }
                if (found) {
                    continue;
                }
                optimizedActions.Add(new List<DeferredAction>() {
                    action
                });
            }
            return optimizedActions;
        }

        void DebugLogSuspiciousAction(int count, DeferredAction action) {
            Debug.LogException(new Exception($"Added {count} th similar element to DeferredAction list. \nScene: {action.SceneReference}\nType: {action.GetType()}\nAction: {action}"));
        }
    }
}