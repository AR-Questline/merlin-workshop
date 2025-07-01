using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class UnityMeshValidator : IStaticRenderingValidator<Mesh> {
        public void Check(Mesh contextObject, List<RenderingErrorMessage> errorMessages) {
            if (AssetDatabase.GetAssetPath(contextObject).StartsWith("Library/unity default resources")) {
                errorMessages.Add(new($"Unity default mesh", RenderingErrorLogType.Exception));
            }
        }
    }
}
