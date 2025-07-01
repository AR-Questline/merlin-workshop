using System.Collections.Generic;
using Awaken.TG.Main.Rendering;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class ImpostorRendererValidator : IStaticRenderingValidator {
        public void Check(in RenderingContextObject context, List<RenderingErrorMessage> errorMessages) {
            if (context.context is Material material) {
                var shader = material.shader;
                if (shader == null) {
                    return;
                }

                if (shader.name.Contains("Impostor")) {
                    errorMessages.Add(new("Impostor", RenderingErrorLogType.Error));
                }
            } else if (context.context is MeshRenderer meshRenderer) {
                if (meshRenderer.gameObject.layer == RenderLayers.Impostor) {
                    errorMessages.Add(new("Impostor", RenderingErrorLogType.Error));
                }
            }
        }
    }
}
