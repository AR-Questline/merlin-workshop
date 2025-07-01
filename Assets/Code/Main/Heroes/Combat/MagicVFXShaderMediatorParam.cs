using Awaken.TG.Graphics.VFX.ShaderControlling;
using Awaken.TG.VisualScripts.Units.VFX;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Heroes.Combat {
    public struct MagicVFXShaderMediatorParam : IApplicableToVFX {
        readonly float _duration;

        public MagicVFXShaderMediatorParam(float duration) {
            _duration = duration;
        }

        public void ApplyToVFX(VisualEffect vfx, GameObject gameObject) {
            if (gameObject != null) {
                gameObject.GetComponentInChildren<ShaderControllerMediator>().SetDuration(_duration);
            } else {
                vfx.GetComponentInChildren<ShaderControllerMediator>().SetDuration(_duration);
            }
        }
    }
}