using UnityEngine;

namespace Awaken.TG.Graphics {
    [RequireComponent(typeof(Light))]
    public class ActivationLink : MonoBehaviour {
        public Light linkedLight;
        public Camera referenceCamera;

        void Awake() {
            if (linkedLight == null) {
                linkedLight = GetComponent<Light>();
            }

            if (linkedLight == null || referenceCamera == null) {
                Destroy(this);
            }
        }
        void LateUpdate() {
            if (linkedLight == null || referenceCamera == null) {
                Destroy(this);
                return;
            }
            linkedLight.enabled = referenceCamera.enabled && referenceCamera.gameObject.activeInHierarchy;
        }
    }
}