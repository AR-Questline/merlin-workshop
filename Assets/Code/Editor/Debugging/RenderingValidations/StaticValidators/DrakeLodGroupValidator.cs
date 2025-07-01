using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class DrakeLodGroupValidator : IStaticRenderingValidator<DrakeLodGroup> {
        public void Check(DrakeLodGroup contextObject, List<RenderingErrorMessage> errorMessages) {
            if (!contextObject.gameObject.isStatic) {
                errorMessages.Add(new("Drake LOD group is non static", RenderingErrorLogType.Exception));
            }
            if (!contextObject.IsBaked) {
                errorMessages.Add(new("Drake LOD group is not baked properly", RenderingErrorLogType.Exception));
            }
            if (contextObject.Renderers.Length == 0) {
                errorMessages.Add(new("Drake LOD group is empty", RenderingErrorLogType.Exception));
            }
        }
    }
}
