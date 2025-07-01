using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public readonly struct ElevatorData {
        public readonly Vector3 targetPoint;
        public readonly bool instant;
        
        public ElevatorData(Vector3 targetPoint, bool instant = false) {
            this.targetPoint = targetPoint;
            this.instant = instant;
        }
    }
}