using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    public class VFXRandomSeed : MonoBehaviour {
        VisualEffect _vfx;
        const string SeedPropertyName = "Seed";

        void Awake() {
            _vfx = GetComponent<VisualEffect>();
        }

        void OnEnable() {
            if (_vfx == null)
                return;
            if (_vfx.HasFloat(SeedPropertyName))
                _vfx.SetFloat(SeedPropertyName, Random.Range(-1000, 1000));
        }
    }
}