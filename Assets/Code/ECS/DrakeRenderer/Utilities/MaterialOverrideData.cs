using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Utilities {
    public unsafe struct MaterialOverrideData {
        public fixed float data[4];

        // Runtime data
        public readonly TypeIndex typeIndex;

        [UnityEngine.Scripting.Preserve] public readonly bool IsEmpty => typeIndex == TypeIndex.Null;
        public readonly ComponentType ComponentType => ComponentType.FromTypeIndex(typeIndex);
        public readonly TypeManager.TypeInfo TypeInfo => TypeManager.GetTypeInfo(typeIndex);

        public MaterialOverrideData(TypeIndex typeIndex, float x, float y, float z, float w) {
            this.data[0] = x;
            this.data[1] = y;
            this.data[2] = z;
            this.data[3] = w;
            this.typeIndex = typeIndex;
        }

        public MaterialOverrideData(TypeIndex typeIndex, float x) {
            this.data[0] = x;
            this.data[1] = 0;
            this.data[2] = 0;
            this.data[3] = 0;
            this.typeIndex = typeIndex;
        }

        [UnityEngine.Scripting.Preserve]
        public MaterialOverrideData(TypeIndex typeIndex, IReadOnlyList<float> data) {
            this.data[0] = data[0];
            this.data[1] = data[1];
            this.data[2] = data[2];
            this.data[3] = data[3];
            this.typeIndex = typeIndex;
        }

        [UnityEngine.Scripting.Preserve]
        public MaterialOverrideData(TypeIndex typeIndex, float4 data) {
            this.data[0] = data.x;
            this.data[1] = data.y;
            this.data[2] = data.z;
            this.data[3] = data.w;
            this.typeIndex = typeIndex;
        }

        public readonly void AddComponent(Entity entity, ref EntityCommandBuffer ecb) {
            fixed (float* dataPtr = data) {
                ecb.UnsafeAddComponent(entity, typeIndex, TypeInfo.TypeSize, dataPtr);
            }
        }

        public readonly void SetComponent(Entity entity, ref EntityCommandBuffer ecb) {
            fixed (float* dataPtr = data) {
                ecb.UnsafeSetComponent(entity, typeIndex, TypeInfo.TypeSize, dataPtr);
            }
        }

        public readonly void RemoveComponent(Entity entity, ref EntityCommandBuffer ecb) {
            ecb.RemoveComponent(entity, ComponentType.FromTypeIndex(typeIndex));
        }

        public void SetValue(float value) {
            data[0] = value;
        }

        [UnityEngine.Scripting.Preserve]
        public void SetValue(Color value) {
            data[0] = value.r;
            data[1] = value.g;
            data[2] = value.b;
            data[3] = value.a;
        }
    }
}