using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.VFX {
    [ExecuteInEditMode]
    public class WyrdNightSphereController : MonoBehaviour {
        [UnityEngine.Scripting.Preserve] public Volume volume;
        [UnityEngine.Scripting.Preserve] public GameObject sphere;
        
        //WyrdnessPostProcess _wyrdnessPostProcess;

        // void Start() {
        //     if (volume == null) {
        //         Debug.LogError("Volume not assigned");
        //         enabled = false;
        //         return;
        //     }
        //
        //     if (!volume.sharedProfile.TryGet(out _wyrdnessPostProcess)) {
        //         Debug.LogError("No WyrdnessPostProcess component found in the assigned Volume.");
        //         enabled = false;
        //     }
        // }
        //
        // void Update() {
        //     if (volume.sharedProfile.TryGet(out _wyrdnessPostProcess)) {
        //         _wyrdnessPostProcess.spherePosition.value = transform.position;
        //         if (sphere != null) {
        //             _wyrdnessPostProcess.sphereScale.value = sphere.transform.localScale.x;
        //         }
        //     }
        // }
    }
}