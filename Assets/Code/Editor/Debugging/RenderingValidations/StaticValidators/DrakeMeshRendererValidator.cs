using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;
using Unity.Mathematics;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class DrakeMeshRendererValidator : IStaticRenderingValidator<DrakeMeshRenderer> {
        public void Check(DrakeMeshRenderer contextObject, List<RenderingErrorMessage> errorMessages) {
            if (!contextObject.Parent) {
                errorMessages.Add(new("Drake mesh renderer without LOD group", RenderingErrorLogType.Exception));
                if (!contextObject.gameObject.isStatic) {
                    errorMessages.Add(new("Drake mesh renderer is non static", RenderingErrorLogType.Exception));
                }
            }
            if (!contextObject.IsBaked) {
                errorMessages.Add(new("Drake mesh renderer is not baked properly", RenderingErrorLogType.Exception));
            }
            if (math.determinant(contextObject.LocalToWorld) < 0) {
                errorMessages.Add(new("Negative scale", RenderingErrorLogType.Exception));
            }
        }
    }
}
