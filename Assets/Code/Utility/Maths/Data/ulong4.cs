using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    public struct ulong4 {
        public ulong x;
        public ulong y;
        public ulong z;
        public ulong w;

        public ulong4(ulong x, ulong y, ulong z, ulong w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static ulong4 operator |(ulong4 a, ulong4 b) {
            unchecked {
                return new ulong4(a.x | b.x, a.y | b.y, a.z | b.z, a.w | b.w);
            }
        }

        public static ulong4 operator &(ulong4 a, ulong4 b) {
            unchecked {
                return new ulong4(a.x & b.x, a.y & b.y, a.z & b.z, a.w & b.w);
            }
        }

        public static ulong4 operator &(ulong4 a, ulong b) {
            unchecked {
                return new ulong4(a.x & b, a.y & b, a.z & b, a.w & b);
            }
        }

        public static ulong4 operator &(ulong a, ulong4 b) {
            unchecked {
                return new ulong4(a & b.x, a & b.y, a & b.z, a & b.w);
            }
        }

        public static bool4 operator ==(ulong4 a, ulong b) {
            unchecked {
                return new bool4 (a.x == b, a.y == b, a.z == b, a.w == b);
            }
        }

        public static bool4 operator !=(ulong4 a, ulong b) {
            unchecked {
                return new bool4 (a.x != b, a.y != b, a.z != b, a.w != b);
            }
        }

        public override unsafe int GetHashCode() {
            uint2x4 toHash = new uint2x4();
            UnsafeUtility.CopyStructureToPtr(ref this, &toHash);
            return unchecked((int)math.hash(toHash));
        }
    }
}