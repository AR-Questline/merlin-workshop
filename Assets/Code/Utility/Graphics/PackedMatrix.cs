using System;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Graphics {
    [Serializable]
    public struct PackedMatrix {
        public const int Stride = (4*3) * 4; // 4 rows of 3 floats of 4 bytes each

        // ReSharper disable InconsistentNaming
        public float c0x;
        public float c0y;
        public float c0z;
        public float c1x;
        public float c1y;
        public float c1z;
        public float c2x;
        public float c2y;
        public float c2z;
        public float c3x;
        public float c3y;
        public float c3z;
        // ReSharper restore InconsistentNaming

        public PackedMatrix(Matrix4x4 m) {
            c0x = m.m00;
            c0y = m.m10;
            c0z = m.m20;
            c1x = m.m01;
            c1y = m.m11;
            c1z = m.m21;
            c2x = m.m02;
            c2y = m.m12;
            c2z = m.m22;
            c3x = m.m03;
            c3y = m.m13;
            c3z = m.m23;
        }

        public PackedMatrix(float4x4 m) {
            c0x = m.c0.x;
            c0y = m.c0.y;
            c0z = m.c0.z;
            c1x = m.c1.x;
            c1y = m.c1.y;
            c1z = m.c1.z;
            c2x = m.c2.x;
            c2y = m.c2.y;
            c2z = m.c2.z;
            c3x = m.c3.x;
            c3y = m.c3.y;
            c3z = m.c3.z;
        }

        public PackedMatrix Inverse() {
            var inverse = math.inverse(ToFloat4x4());
            return new PackedMatrix(inverse);
        }

        public float4x4 ToFloat4x4() {
            return new float4x4(
                new float4(c0x, c0y, c0z, 0),
                new float4(c1x, c1y, c1z, 0),
                new float4(c2x, c2y, c2z, 0),
                new float4(c3x, c3y, c3z, 1)
                );
        }
    }
}
