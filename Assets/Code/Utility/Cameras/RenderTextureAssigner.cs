using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Utility.Cameras {
    public class RenderTextureAssigner : MonoBehaviour {
        public RenderTextureHandle handle;
        public Camera targetCamera;
        public RawImage targetImage;
        
        void Awake() {
            handle.Get(this);
        }

        void Update() {
            handle.Check(Screen.width, Screen.height);
        }

        public void Assign(RenderTexture texture) {
            if (targetCamera != null) {
                targetCamera.targetTexture = texture;
            }
            if (targetImage != null) {
                targetImage.texture = texture;
            }
        }
    }
}