using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class NonStaticMeshRenderersValidator : IStaticRenderingValidator<MeshRenderer> {
        public void Check(MeshRenderer contextObject, List<RenderingErrorMessage> errorMessages) {
            if (!contextObject.gameObject.isStatic) {
                errorMessages.Add(new("MeshRenderer is not static", RenderingErrorLogType.Exception));
            }
        }
    }
}
