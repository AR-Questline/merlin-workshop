using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class HasDefaultDecalLayer : IStaticRenderingValidator
    {
#pragma warning disable CS0618 // Type or member is obsolete
        const uint DecalLayerMask = (uint)UnityEngine.Rendering.HighDefinition.RenderingLayerMask.DecalLayerDefault;
#pragma warning restore CS0618 // Type or member is obsolete

        public void Check(in RenderingContextObject context, List<RenderingErrorMessage> errorMessages) {
            if (context.context is MeshRenderer meshRenderer) {
                if ((meshRenderer.renderingLayerMask & DecalLayerMask) != DecalLayerMask) {
                    errorMessages.Add(new("Default decal layer is disabled", RenderingErrorLogType.Error));
                }
            } else if (context.context is DrakeMeshRenderer drakeMeshRenderer) {
                var renderingLayers = drakeMeshRenderer.RenderMeshDescription(true).FilterSettings.RenderingLayerMask;
                if ((renderingLayers & DecalLayerMask) != DecalLayerMask) {
                    errorMessages.Add(new("Default decal layer is disabled", RenderingErrorLogType.Error));
                }
            }
        }
    }
}