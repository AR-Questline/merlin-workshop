using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// This script exists to allow set lifetime of VFX directly inside VFX instead in prefab that references it.
    /// </summary>
    public class VFXLifetime : MonoBehaviour {
        [UnityEngine.Scripting.Preserve] public const float DefaultVFXLifeTime = 10;
        [Range(0.01f, 100f)] public float lifeTime = 10;
    }
}
