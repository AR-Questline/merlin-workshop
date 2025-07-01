using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    [System.Serializable]
    public struct CompressedTRSMatrix {
        public float3 position;
        public CompressedQuaternion rotation;
        public half3 scale;

        public CompressedTRSMatrix(float3 position, CompressedQuaternion rotation, half3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public CompressedTRSMatrix(float4x4 matrix) {
            position = matrix.Translation();
            rotation = (CompressedQuaternion)matrix.Rotation();
            scale = (half3)matrix.Scale();
        }

        public static explicit operator CompressedTRSMatrix(float4x4 matrix) => new(
            matrix.Translation(),
            (CompressedQuaternion)matrix.Rotation(), 
            (half3)matrix.Scale());

        public static implicit operator float4x4(CompressedTRSMatrix compressed) =>
            float4x4.TRS(compressed.position, compressed.rotation, compressed.scale);
    }
}