using System;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Awaken.ECS.Authoring {
    [Serializable]
    public struct LodGroupSerializableData {
        public void Initialize(LODGroup lodGroup) {
            throw new NotImplementedException();
        }

        public readonly LodGroupSerializableData WithLocalToWorldMatrix(float4x4 overrideLocalToWorldMatrix) {
            throw new NotImplementedException();
        }

        public readonly MeshLODGroupComponent ToLodGroupComponent() {
            throw new NotImplementedException();
        }

        public readonly LocalToWorld ToTransformComponent() {
            throw new NotImplementedException();
        }

        public readonly LODWorldReferencePoint ToWorldReferencePoint() {
            throw new NotImplementedException();
        }

        public readonly LODRange CreateLODRange(int lodMask) {
            throw new NotImplementedException();
        }

        public readonly float GetMaxRenderingDistance() {
            throw new NotImplementedException();
        }

        public float GetLODDistance(int lodIndex) {
            throw new NotImplementedException();
        }

        public readonly int LastValidLodIndex() {
            throw new NotImplementedException();
        }
    }
}