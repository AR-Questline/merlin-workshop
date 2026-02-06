using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Utilities {
    public unsafe struct MaterialOverrideData {
        public fixed float data[4];
        public readonly TypeIndex typeIndex;
        public readonly bool IsEmpty => throw new NotImplementedException();
        public readonly ComponentType ComponentType => throw new NotImplementedException();
        public readonly TypeManager.TypeInfo TypeInfo => throw new NotImplementedException();

        public MaterialOverrideData(TypeIndex typeIndex, float x, float y, float z, float w) {
            throw new NotImplementedException();
        }

        public MaterialOverrideData(TypeIndex typeIndex, float x) {
            throw new NotImplementedException();
        }

        public MaterialOverrideData(TypeIndex typeIndex, IReadOnlyList<float> data) {
            throw new NotImplementedException();
        }

        public MaterialOverrideData(TypeIndex typeIndex, float4 data) {
            throw new NotImplementedException();
        }

        public readonly void AddComponent(Entity entity, ref EntityCommandBuffer ecb) {
            throw new NotImplementedException();
        }

        public readonly void SetComponent(Entity entity, ref EntityCommandBuffer ecb) {
            throw new NotImplementedException();
        }

        public readonly void RemoveComponent(Entity entity, ref EntityCommandBuffer ecb) {
            throw new NotImplementedException();
        }

        public void SetValue(float value) {
            throw new NotImplementedException();
        }

        public void SetValue(Color value) {
            throw new NotImplementedException();
        }
    }
}