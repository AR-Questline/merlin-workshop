using System.Collections.Generic;
using Awaken.Utility.Maths;
using Awaken.Utility.Previews;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class KandraPreview : IARRendererPreview {
        readonly KandraRenderer _renderer;
        readonly Mesh _mesh;

        public bool IsValid => _renderer && _mesh;
        public Mesh Mesh => _mesh;
        public Material[] Materials => _renderer.rendererData.materials;
        public Bounds WorldBounds => GetWorldBounds(_renderer);
        public Matrix4x4 Matrix => Matrix4x4.identity;

        public KandraPreview(KandraRenderer renderer) {
            _renderer = renderer;
            _mesh = _renderer.BakePoseMesh();
        }

        public void Dispose() {
            Object.DestroyImmediate(_mesh);
        }

        static Bounds GetWorldBounds(KandraRenderer renderer) {
            var mesh = renderer.rendererData.mesh;
            var localBounds = mesh.meshLocalBounds;
            var matrix = RendererLocalToWorld(renderer);
            return localBounds.Transform(matrix);
        }

        static Matrix4x4 RendererLocalToWorld(KandraRenderer renderer) {
            var rig = renderer.rendererData.rig;
            var rootBoneIndex = renderer.rendererData.rootBone;
            var rootPose = renderer.rendererData.rootBoneMatrix;
            var expandedPose = new float4x4(
                new float4(rootPose.c0.xyz, 0),
                new float4(rootPose.c1.xyz, 0),
                new float4(rootPose.c2.xyz, 0),
                new float4(rootPose.c3.xyz, 1f)
            );
            var rootBone = rig.bones[rootBoneIndex];
            return math.mul(rootBone.localToWorldMatrix, expandedPose);
        }

        // === Preview creator
        [InitializeOnLoadMethod]
        static void RegisterPreview() {
            KandraRenderer.PreviewCreator = GetPreviews;
        }

        static IEnumerable<IARRendererPreview> GetPreviews(KandraRenderer kandraRenderer) {
            yield return new KandraPreview(kandraRenderer);
        }
    }
}
