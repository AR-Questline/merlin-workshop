using System;
using Awaken.Utility.Graphics;
using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public struct SmallTransform {
        public float3 position;
        public quaternionHalf rotation;
        public half3 scale;

        public SmallTransform(float3 position, quaternion rotation, float3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = new half3(new half(scale.x), new half(scale.y), new half(scale.z));
        }

        public SmallTransform(float3 position, quaternion rotation, half3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public PackedMatrix ToPackedMatrix() {
            return new PackedMatrix(float4x4.TRS(position, rotation, scale));
        }

        public float4x4 ToFloat4x4() {
            return float4x4.TRS(position, rotation, scale);
        }
    }

    // This is synchronized with GPU data so need to be aligned to 4 bytes
    // If you change this struct you need to change shader code too
    public struct SmallTransformWithPadding {
        [UnityEngine.Scripting.Preserve]  public float3 position;
        [UnityEngine.Scripting.Preserve]  public quaternionHalf rotation;
        [UnityEngine.Scripting.Preserve]  public half3 scale;
        [UnityEngine.Scripting.Preserve]  public ushort padding; // For GPU alignment

        [UnityEngine.Scripting.Preserve] 
        public SmallTransformWithPadding(SmallTransform transform) {
            this.position = transform.position;
            this.rotation = transform.rotation;
            this.scale = transform.scale;
            this.padding = 0;
        }
    }
}
