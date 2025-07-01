using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class CommuteToInteraction : CommuteToBase {
        public override bool StopOnReach => true;
        
        public void Setup(Vector3 position, float positionRange, float exitRadiusSq) {
            SetupInternal(position, positionRange, exitRadiusSq);
        }
    }
}