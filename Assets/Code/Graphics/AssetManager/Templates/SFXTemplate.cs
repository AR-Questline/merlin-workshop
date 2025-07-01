using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG._3DAssets.AssetManager.Templates
{
    [CreateAssetMenu(fileName = "SFX_", menuName = "Asset Manager/SFX Data")] [UnityEngine.Scripting.Preserve]
    public class SFXTemplate : ScriptableObject {
        [LabelWidth(80), Required]
        [UnityEngine.Scripting.Preserve] public AudioClip audioClip;

        [UnityEngine.Scripting.Preserve] public float volume = 1f;
        [UnityEngine.Scripting.Preserve] public float volumeVariation = 0.05f;
        [UnityEngine.Scripting.Preserve] public float pitch = 1f;
        [UnityEngine.Scripting.Preserve] public float pitchVariation = 0.05f;
    }
}
