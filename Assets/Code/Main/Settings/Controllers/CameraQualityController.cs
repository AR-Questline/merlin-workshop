using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class CameraQualityController : MonoBehaviour {
        void Start() {
            Add<ReflectionsCameraController>();
            Add<ShadowsCameraController>();
            Add<DistanceCullingCameraController>();
            Add<SSAOCameraController>();
            Add<SSSCameraController>();
            Add<AntiAliasingController>();
            Add<UpScalingController>();
        }

        void Add<T>() where T : Component {
            if (gameObject.GetComponent<T>() == null) {
                gameObject.AddComponent<T>();
            }
        }
    }
}
