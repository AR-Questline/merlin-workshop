using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Main.Settings.Controllers {
    /// <summary>
    /// One component that spawns all quality controllers for PostProcessing Volumes
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class VolumeQualityController : MonoBehaviour {
        void Start() {
            Add<FogController>();
            Add<SSAOController>();
            Add<ChromaticAberrationController>();
            Add<OldGpuFixer>();
            Add<CloudsShadowsResolutionController>();
        }

        void Add<T>() where T : Component {
            if (gameObject.GetComponent<T>() == null) {
                gameObject.AddComponent<T>();
            }
        }
    }
}