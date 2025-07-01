using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class OrdinalMeshRendererValidator : IStaticRenderingValidator<MeshRenderer> {
        public void Check(MeshRenderer meshRenderer, List<RenderingErrorMessage> errorMessages) {
            var parentOptimizationSystem = meshRenderer.GetComponentInParent<IRenderingOptimizationSystem>(true);
            if (parentOptimizationSystem == null || !parentOptimizationSystem.Has(meshRenderer)) {
                errorMessages.Add(new($"MeshRenderer is not part of any rendering optimization system", RenderingErrorLogType.Exception));
            }
            if (math.cmin(meshRenderer.transform.lossyScale) <= 0) {
                errorMessages.Add(new($"MeshRenderer has negative scale", RenderingErrorLogType.Exception));
            }
        }
    }
}
