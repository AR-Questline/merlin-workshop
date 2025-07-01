using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.Controllers {
    public class Force {
        public readonly Vector3 direction;
        public float duration;

        public Force(Vector3 direction, float duration) {
            this.direction = direction;
            this.duration = duration;
        }
    }
}