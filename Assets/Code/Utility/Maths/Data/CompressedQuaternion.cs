using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    public struct CompressedQuaternion {
        public uint value;

        public CompressedQuaternion(uint value) {
            this.value = value;
        }
        
        public static implicit operator float4(CompressedQuaternion compressed) => CompressionUtils.DecompressQuaternion(compressed.value).value;
        public static implicit operator quaternion(CompressedQuaternion compressed) => CompressionUtils.DecompressQuaternion(compressed.value);
        public static implicit operator Quaternion(CompressedQuaternion compressed) => CompressionUtils.DecompressQuaternion(compressed.value);
        
        public static explicit operator CompressedQuaternion(float4 value) => (CompressedQuaternion)CompressionUtils.CompressQuaternion(value);
        public static explicit operator CompressedQuaternion(quaternion quaternion) => (CompressedQuaternion)CompressionUtils.CompressQuaternion(quaternion);
        public static explicit operator CompressedQuaternion(Quaternion quaternion) => (CompressedQuaternion)(quaternion)quaternion;
        public static explicit operator CompressedQuaternion(uint value) => new(value);
    }
}