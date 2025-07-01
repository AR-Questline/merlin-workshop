using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class MeshPropertiesValidator : IStaticRenderingValidator<Mesh> {
        public const int VertexCountException = 500_000;
        public const int VertexCountError = 100_000;
        public const int VertexCountWarning = 50_000;
        public const int SubMeshCountException = 8;
        public const int SubMeshCountError = 4;
        public const int SubMeshCountWarning = 2;
        
        public void Check(Mesh mesh, List<RenderingErrorMessage> errorMessages) {
            if (mesh.vertexCount > VertexCountException) {
                errorMessages.Add(new("Enormous mesh, way too much vertices", RenderingErrorLogType.Exception));
            } else if (mesh.vertexCount > VertexCountError) {
                errorMessages.Add(new("Large mesh, too much vertices", RenderingErrorLogType.Error));
            } else if (mesh.vertexCount > VertexCountWarning) {
                errorMessages.Add(new("Large mesh, many vertices", RenderingErrorLogType.Warning));
            }

            if (mesh.subMeshCount > SubMeshCountException) {
                errorMessages.Add(new("Enormous count of submeshes", RenderingErrorLogType.Exception));
            } else if (mesh.subMeshCount > SubMeshCountError) {
                errorMessages.Add(new("Many submeshes", RenderingErrorLogType.Error));
            } else if (mesh.subMeshCount > SubMeshCountWarning) {
                errorMessages.Add(new("Has submeshes", RenderingErrorLogType.Warning));
            }

            if (mesh.isReadable) {
                errorMessages.Add(new("Mesh is readable", RenderingErrorLogType.Error));
            }

            if (mesh.indexFormat == IndexFormat.UInt32) {
                errorMessages.Add(new("Mesh uses 32-bit indices", RenderingErrorLogType.Warning));
            }

            if (mesh.GetTopology(0) != MeshTopology.Triangles) {
                errorMessages.Add(new("Mesh has non-triangle topology", RenderingErrorLogType.Exception));
            }

            var isSkinned = mesh.HasVertexAttribute(VertexAttribute.BlendWeight);
            var attributesCount = mesh.vertexAttributeCount;
            var errorMaxAttributes = isSkinned ? 7 : 6;
            var alertMaxAttributes = isSkinned ? 6 : 5;
            if (attributesCount > errorMaxAttributes) {
                errorMessages.Add(new($"Mesh has {attributesCount} vertex attributes", RenderingErrorLogType.Error));
            } else if (attributesCount > alertMaxAttributes) {
                errorMessages.Add(new($"Mesh has {attributesCount} vertex attributes", RenderingErrorLogType.Warning));
            }
        }
    }
}
