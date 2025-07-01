using UnityEngine;

namespace Awaken.TG.Main.Utility {
    public class IgnoreCollisions : MonoBehaviour {
        public Collider collider1, collider2;
        void Awake() {
            Physics.IgnoreCollision(collider1, collider2);
        }
    }
}
