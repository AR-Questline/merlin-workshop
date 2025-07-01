using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.VFX {
    public interface IApplicableToVFXWithMaterial : IApplicableToVFX {
        public void ApplyToShaderMaterial(Material shaderMaterial, GameObject gameObject);
    }
}