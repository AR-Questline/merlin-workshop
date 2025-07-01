using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class StaticSkinnedMeshRendererValidator : IStaticRenderingValidator<SkinnedMeshRenderer> {
        public void Check(SkinnedMeshRenderer skinnedMeshRenderer, List<RenderingErrorMessage> errorMessages) {
            if (skinnedMeshRenderer.gameObject.isStatic) {
                errorMessages.Add(new("SkinnedMeshRenderer is static", RenderingErrorLogType.Exception));
            }
        }
    }
}
