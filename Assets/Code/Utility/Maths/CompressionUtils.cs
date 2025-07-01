using Unity.Mathematics;

namespace Awaken.Utility.Maths {
    public static class CompressionUtils {
        const float StereographicScale = 1.7777f;
        const float M_SQRT1_2 = 0.70710678118f; // sqrt(1/2)

        public static uint EncodeNormalVectorSpheremap(float3 vector) {
            var p = math.sqrt(vector.z*8f+8f);

            var encodedFloatVector = vector.xy / p + 0.5f;

            var encodedHalfVector = math.f32tof16(encodedFloatVector);

            return encodedHalfVector.x | (encodedHalfVector.y << 16);
        }

        public static float3 DecodeNormalVectorSpheremap(uint encodedVector) {
            var halfX = encodedVector & 0xFFFF;
            var halfY = encodedVector >> 16;

            var encodedFloat = math.f16tof32(new uint2(halfX, halfY));

            var fenc = encodedFloat*4f-2f;

            var f = math.dot(fenc.xy, fenc.xy);
            var g = math.sqrt(1f-f/4f);
            var decoded = default(float3);
            decoded.xy = fenc.xy*g;
            decoded.z = 1f-f/2f;
            return math.normalize(decoded);
        }

        public static uint EncodeNormalVectorStereographic(float3 vector) {
            var enc = vector.xy / (vector.z+1f);
            enc /= StereographicScale;
            var encodedFloatVector = enc*0.5f+0.5f;

            var encodedHalfVector = math.f32tof16(encodedFloatVector);

            return encodedHalfVector.x | (encodedHalfVector.y << 16);
        }

        public static float3 DecodeNormalVectorStereographic(uint encodedVector) {
            var halfX = encodedVector & 0xFFFF;
            var halfY = encodedVector >> 16;

            var encodedFloat = new float4(math.f16tof32(new uint2(halfX, halfY)), 0, 0);

            var nn = encodedFloat.xyz * new float3(2*StereographicScale, 2*StereographicScale, 0) + new float3(-StereographicScale, -StereographicScale, 1);
            var g = 2.0f / math.dot(nn.xyz,nn.xyz);
            var n = default(float3);
            n.xy = g*nn.xy;
            n.z = g-1f;
            return n;
        }

        public static uint EncodeNormalVectorOctahedron(float3 vector) {
            var n = vector / (math.abs(vector.x) + math.abs(vector.y) + math.abs(vector.z));
            var encodedFloatVector = n.z >= 0.0f ? n.xy : OctWrap(n.xy);
            encodedFloatVector = encodedFloatVector * 0.5f + 0.5f;

            var encodedHalfVector = math.f32tof16(encodedFloatVector);

            return encodedHalfVector.x | (encodedHalfVector.y << 16);
        }

        public static float3 DecodeNormalVectorOctahedron(uint encodedVector) {
            var halfX = encodedVector & 0xFFFF;
            var halfY = encodedVector >> 16;

            var encodedFloat = math.f16tof32(new uint2(halfX, halfY));

            var f = encodedFloat * 2.0f - 1.0f;

            // https://twitter.com/Stubbesaurus/status/937994790553227264
            var n = new float3(f.x, f.y, 1.0f - math.abs(f.x) - math.abs(f.y));
            var t = math.saturate(-n.z);
            n.xy += math.select(t, -t, n.xy >= 0.0f);
            return math.normalize(n);
        }

        public static uint2 EncodeNormalAndTangent(float3 normal, float3 tangent) {
            var encodedNormal = EncodeNormalVectorOctahedron(normal);
            var encodedTangent = EncodeNormalVectorOctahedron(tangent);
            return new uint2(encodedNormal, encodedTangent);
        }

        public static void DecodeNormalAndTangent(uint2 normalAndTangent, out float3 normal, out float3 tangent) {
            normal = DecodeNormalVectorOctahedron(normalAndTangent.x);
            tangent = DecodeNormalVectorOctahedron(normalAndTangent.y);
        }

        static float2 OctWrap(float2 v) {
            return (1.0f - math.abs(v.yx)) * math.select(-1.0f, 1.0f, v.xy >= 0.0f);
        }
        
        public static uint CompressQuaternion(quaternion quaternion) {
            var qValue = quaternion.value;
            // We send the values of the quaternion's smallest 3 elements.
            uint i_largest = 0;
            for (uint i = 1; i < 4; ++i) {
                if (math.abs(qValue[(int)i]) > math.abs(qValue[(int)i_largest])) {
                    i_largest = i;
                }
            }

            // Since -q represents the same rotation as q,
            // transform the quaternion so the largest element is positive.
            // This avoids having to send its sign bit.
            bool negate = qValue[(int)i_largest] < 0f;

            // 1/sqrt(2) is the largest possible value 
            // of the second-largest element in a unit quaternion.

            // Do compression using sign bit and 9-bit precision per element.
            uint comp = i_largest;
            for (uint i = 0; i < 4; ++i) {
                if (i != i_largest) {
                    bool negbit = (qValue[(int)i] < 0f) ^ negate;
                    uint mag = (uint)(((1 << 9) - 1) * (math.abs(qValue[(int)i]) / M_SQRT1_2) + 0.5f);
                    comp = (comp << 10) | ((negbit ? 1u : 0u) << 9) | (mag & 0x1FFu); // Mask with 9 bits
                }
            }

            return comp;
        }

        public static quaternion DecompressQuaternion(uint comp) {
            const uint mask = (1u << 9) - 1u; // Mask for 9 bits (0x1FF)

            int i_largest = (int)(comp >> 30);
            float sum_squares = 0f;
            var q = new float4(0f);

            for (int i = 3; i >= 0; --i) {
                if (i != i_largest) {
                    uint mag = comp & mask;
                    uint negbit = (comp >> 9) & 0x1u;
                    comp = comp >> 10;
                    q[i] = M_SQRT1_2 * ((float)mag) / mask;
                    if (negbit == 1) {
                        q[i] = -q[i];
                    }

                    sum_squares += q[i] * q[i];
                }
            }

            q[i_largest] = math.sqrt(1.0f - sum_squares);
            return q;
        }
    }
}