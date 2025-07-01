using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    [CreateAssetMenu(menuName = "Scriptable Objects/Seed SO")]
    public class SeedSO : ScriptableObject {
        [SerializeField] public uint seed;
    }
}