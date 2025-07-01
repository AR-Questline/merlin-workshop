using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class InvalidShaderRendererValidator : IStaticRenderingValidator<Material> {
        static readonly string[] InvalidShadersNames = {
            "Shader Graphs/Shader_WindCloth", "KF/Dissolve/KF_Dissolve",
            "Shader Graphs/SG_CharacterShader", "NatureManufacture/HDRP/Foliage/Foliage",
            "Custom/Lordenfel/HDRP/FoliageWind", "NatureManufacture/HDRP/Foliage/Bark",
            "NatureManufacture/HDRP/Foliage/Cross", "Hidden/Amplify Impostors/Octahedron Impostor HDRP"
        };

        public void Check(Material material, List<RenderingErrorMessage> errorMessages) {
            var shader = material.shader;
            if (shader == null) {
                errorMessages.Add(new("Material has no shader", RenderingErrorLogType.Error));
            }

            var name = shader.name;
            if (Array.IndexOf(InvalidShadersNames, name) > -1) {
                errorMessages.Add(new($"Has shader: {name}", RenderingErrorLogType.Error));
            }
        }
    }
}
