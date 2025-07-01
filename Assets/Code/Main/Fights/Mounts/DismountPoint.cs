using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public class DismountPoint : MonoBehaviour {
        public bool isAvailable = true;

        void OnTriggerEnter(Collider other) {
            isAvailable = false;
        }

        void OnTriggerStay(Collider other) {
            isAvailable = false;
        }

        void OnTriggerExit(Collider other) {
            isAvailable = true;
        }
    }
}
