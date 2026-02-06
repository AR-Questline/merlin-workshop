using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Deferred {
    public partial class DeferredActionWaitForVisualData {
        readonly DeferredActionRequiringVisualLoaded _action;
        readonly WeakModelRef<Location> _locationRef;
        bool _visualLoaed;
        bool _correctPosition;
        IEventListener _movedListener;
        
        public DeferredActionWaitForVisualData(DeferredActionRequiringVisualLoaded action, Location location ) {
            _action = action;
            _locationRef = new WeakModelRef<Location>(location);
        }

        public bool IsStillValid() => _locationRef.TryGet(out _);

        public void Init() {
            if (!_locationRef.TryGet(out var loc)) {
                Complete();
            }
            _visualLoaed = loc.IsVisualLoaded;
            _correctPosition = !NpcPresence.InAbyss(loc.Coords);
            if (!_visualLoaed) {
                loc.OnVisualLoaded(OnVisualLoaded);
            }
            if (!_correctPosition) {
                _movedListener = loc.ListenTo(GroundedEvents.AfterMoved, AfterMoved, loc);
            }
        }
        
        void OnVisualLoaded(Transform _) {
            _visualLoaed = true;
            TryComplete();
        }

        void AfterMoved(IGrounded grounded) {
            _correctPosition = !NpcPresence.InAbyss(grounded.Coords);
            if (_correctPosition) {
                World.EventSystem.TryDisposeListener(ref _movedListener);
                TryComplete();
            }
        }

        void TryComplete() {
            if (_correctPosition && _visualLoaed) {
                Complete();
            }
        }

        public void Destroy() {
            World.EventSystem.TryDisposeListener(ref _movedListener);
        }
        
        void Complete() {
            _action.OnComplete(this);
        }
    }
}