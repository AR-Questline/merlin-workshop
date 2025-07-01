using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public readonly struct VisionDetectionSetup {
        public readonly Vector3 point;
        /// <summary>
        /// Detection cast half extent. So 0 mean raycast and bigger value makes detection easier to fail
        /// </summary>
        public readonly float halfExtent;
        public readonly VisionDetectionTargetType type;

        public VisionDetectionSetup(Vector3 point, float halfExtent, VisionDetectionTargetType type) {
            this.point = point;
            this.halfExtent = halfExtent;
            this.type = type;
        }
    }
}
