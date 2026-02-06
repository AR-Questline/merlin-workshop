using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.Utility.Collections;
using SceneReference = Awaken.TG.Assets.SceneReference;
using World = Awaken.TG.MVC.World;

namespace Awaken.TG.Main.Locations.Deferred {
    public abstract partial class DeferredActionRequiringVisualLoaded : DeferredAction {
        StructList<DeferredActionWaitForVisualData> _waitList;

        protected override bool CanBeExecuted => !IsWaitListValid();
        protected int ListenersCount => _waitList.Count;
        
        protected DeferredActionRequiringVisualLoaded(IEnumerable<DeferredCondition> conditions, SceneReference sceneReference = null) : base(conditions, sceneReference) { }

        bool IsWaitListValid() {
            if (!_waitList.IsCreated) {
                return false;
            }

            for (int i = _waitList.Count - 1; i >= 0; i--) {
                if (!_waitList[i].IsStillValid()) {
                    _waitList[i].Destroy();
                    _waitList.RemoveAtSwapBack(i);
                }
            }

            if (_waitList.Count > 0) {
                return true;
            }

            _waitList.Uncreate();
            return false;
        }
        
        protected void WaitForVisualLoaded(Location location) {
            if (!_waitList.IsCreated) {
                _waitList = new StructList<DeferredActionWaitForVisualData>(1);
            }
            var waitData = new DeferredActionWaitForVisualData(this, location);
            _waitList.Add(waitData);
            waitData.Init();
        }
        
        public void OnComplete(DeferredActionWaitForVisualData waitData) {
            if (!_waitList.IsCreated) {
                return;
            }
            _waitList.Remove(waitData);
            if (_waitList.Count == 0 && World.Any<DeferredSystem>() is {} deferredSystem) {
                _waitList.Uncreate();
                deferredSystem.TryRefreshAction(this);
            }
        }

        protected static bool RequireWait(DeferredLocationExecution execution, Location location) {
            if (!execution.RequireVisualLoaded) {
                return false;
            }
            if (!location.IsVisualLoaded) {
                return true;
            }
            if (NpcPresence.InAbyss(location.Coords)) {
                return true;
            }
            return false;
        }
    }
}