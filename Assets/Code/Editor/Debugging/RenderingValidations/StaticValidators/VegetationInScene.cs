using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class VegetationInScene : IStaticRenderingValidator<Material> {
        public void Check(Material material, List<RenderingErrorMessage> errorMessages) {
            if (IsVegetationShader(material)) {
                errorMessages.Add(new("Vegetation in scene", RenderingErrorLogType.Error));
            }
        }
        static bool IsVegetationShader(Material material) {
            var shader = material.shader;
            if (shader == null) {
                return false;
            }
            var name = shader.name;
            return name.Contains("Nature") || name.Contains("Vegetation");
        }
    }
}
