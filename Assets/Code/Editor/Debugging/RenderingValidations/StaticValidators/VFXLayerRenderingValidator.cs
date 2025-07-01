using System.Collections.Generic;
using Awaken.TG.Main.Rendering;
using Awaken.Utility;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class VFXLayerRenderingValidator : IStaticRenderingValidator<VisualEffect> {
        public void Check(VisualEffect effect, List<RenderingErrorMessage> errorMessages) {
            var layer = effect.gameObject.layer;
            if (layer != RenderLayers.VFX) {
                errorMessages.Add(new($"VFX is on {LayerMask.LayerToName(layer)} instead of VFX layer", RenderingErrorLogType.Error));
            }
        }
    }
}
