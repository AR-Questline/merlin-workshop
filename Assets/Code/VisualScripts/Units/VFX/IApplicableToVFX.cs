using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.VisualScripts.Units.VFX {
    public interface IApplicableToVFX {
        void ApplyToVFX(VisualEffect vfx, GameObject gameObject);
    }
}