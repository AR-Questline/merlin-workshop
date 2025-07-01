using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.UI.RawImageRendering {
    public class ScaleToAspectRatio : MonoBehaviour {
        [SerializeField] Axis axis;
        
        void OnEnable() {
            var camera = World.Only<CameraStateStack>().MainCamera;
            var scale = transform.localScale;
            if (axis == Axis.Horizontal) {
                scale.x = scale.y * camera.aspect;
            } else {
                scale.y = scale.x / camera.aspect;
            }
            transform.localScale = scale;
        }

        enum Axis : byte {
            Horizontal,
            Vertical,
        }
    }
}