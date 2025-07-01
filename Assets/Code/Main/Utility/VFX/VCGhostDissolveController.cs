using Awaken.Kandra;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public class VCGhostDissolveController : VCDissolveDeathRelatedControllerBase<KandraRenderer> {
        static readonly int TransitionProperty = Shader.PropertyToID("_Ghost_Transparency");
        static readonly int PSLProperty = Shader.PropertyToID("_EnableBlendModePreserveSpecularLighting");
        
        protected override float Invisible => inverseTransition ? 1 : 0;
        protected override float Visible => inverseTransition ? 0 : 1;
        
        protected override void BeforeAppeared() {
            TogglePreserveSpecularLighting(true);
            base.BeforeAppeared();
        }

        protected override void BeforeDisappeared() {
            TogglePreserveSpecularLighting(false);
            base.BeforeDisappeared();
        }

        /// <param name="transition">0 means dissolved and 1 means fully visible. It's made this way because of ghost shader</param>
        protected override void UpdateEffects(float transition) {
            for (int i = _actualRenderers.Count - 1; i >= 0; i--) {
                KandraRenderer r = _actualRenderers[i];
                if (r == null) {
                    _actualRenderers.RemoveAtSwapBack(i);
                    continue;
                }

                var materials = r.rendererData.RenderingMaterials;
                foreach (var material in materials) {
                    material.SetFloat(TransitionProperty, transition);
                }
            }
        }

        void TogglePreserveSpecularLighting(bool enabled) {
            for (int i = _actualRenderers.Count - 1; i >= 0; i--) {
                KandraRenderer r = _actualRenderers[i];
                if (r == null) {
                    _actualRenderers.RemoveAtSwapBack(i);
                    continue;
                }
                var materials = r.rendererData.RenderingMaterials;
                foreach (var material in materials) {
                    material.SetFloat(PSLProperty, enabled ? 1 : 0);
                }
            }
        }

        protected override bool CanBeDissolved(KandraRenderer dissolvable) {
            return true;
        }
    }
}