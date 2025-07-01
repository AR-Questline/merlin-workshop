using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class ReadableTexturesValidator : IStaticRenderingValidator<Material> {
        public void Check(Material material, List<RenderingErrorMessage> errorMessages) {
            var ids = material.GetTexturePropertyNameIDs();
            for (int i = 0; i < ids.Length; i++) {
                var texture = material.GetTexture(ids[i]);
                if (texture is Texture2D { isReadable: true }) {
                    errorMessages.Add(new($"Texture {texture.name} is readable", RenderingErrorLogType.Exception));
                }
            }
        }
    }
}