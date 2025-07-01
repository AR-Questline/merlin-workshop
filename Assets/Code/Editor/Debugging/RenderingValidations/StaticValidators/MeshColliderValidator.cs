using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    public class MeshColliderValidator : IStaticRenderingValidator<MeshCollider> {
        public const int VertexCountException = 10_000;
        public const int VertexCountError = 5_000;
        public const int VertexCountWarning = 1_000;
        
        // ReSharper disable once UnusedType.Global
        public void Check(MeshCollider meshCollider, List<RenderingErrorMessage> errorMessages) {
            if (meshCollider.gameObject.GetComponents<MeshCollider>().Length > 1) {
                errorMessages.Add(new("Multiple MeshColliders", RenderingErrorLogType.Error));
            }

            var mesh = meshCollider.sharedMesh;
            if (mesh == null) {
                errorMessages.Add(new("MeshCollider has null mesh", RenderingErrorLogType.Error));
                return;
            }

            if (mesh.vertexCount > VertexCountException) {
                errorMessages.Add(new($"MeshCollider has {mesh.vertexCount} vertices", RenderingErrorLogType.Exception));
            } else if (mesh.vertexCount > VertexCountError) {
                errorMessages.Add(new($"MeshCollider has {mesh.vertexCount} vertices", RenderingErrorLogType.Error));
            } else if (mesh.vertexCount > VertexCountWarning) {
                errorMessages.Add(new($"MeshCollider has {mesh.vertexCount} vertices", RenderingErrorLogType.Warning));
            }
        }
    }
}
