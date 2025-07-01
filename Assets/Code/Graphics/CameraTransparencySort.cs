using UnityEngine;

namespace Awaken.TG.Graphics
{
    public class CameraTransparencySort : MonoBehaviour {
        
        public TransparencySortMode sortMode;
        public Vector3 sortAxis;

        void Awake() {
            OnValidate();
        }

        void OnValidate() {
            Camera cam = GetComponent<Camera>();
            cam.transparencySortMode = sortMode;
            cam.transparencySortAxis = sortAxis;
        }
    }
}
