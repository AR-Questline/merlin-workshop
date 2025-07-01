using Awaken.TG.Main.Grounds;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class CommuteToLocation : CommuteToBase {
        const float UpdateDistanceSqr = 1f * 1f;
        
        IGrounded _grounded;
        readonly bool _stopOnReach;
        
        public override bool StopOnReach => _stopOnReach;
        
        public CommuteToLocation(bool stopOnReach) {
            _stopOnReach = stopOnReach;
        }
        
        public void Setup(IGrounded grounded, float positionRange, float exitRadiusSq) {
            _grounded = grounded;
            SetupInternal(grounded.Coords, positionRange, exitRadiusSq);
        }
        
        public override void UnityUpdate() {
            Vector3 groundedCoords = _grounded.Coords;
            if ((_inactiveStrategy.Position - groundedCoords).sqrMagnitude > UpdateDistanceSqr) {
                _inactiveStrategy.UpdatePosition(groundedCoords);
                _activeStrategy.UpdatePosition(groundedCoords);
            }
            base.UnityUpdate();
        }
    }
}