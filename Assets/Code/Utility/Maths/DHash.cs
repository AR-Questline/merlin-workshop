using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Awaken.Utility.Maths {
    /// <summary> Deterministic HashCode utils </summary>
    public static class DHash {
        public static int Combine(int hash1, int hash2) {
            unchecked {
                int hash = hash1;
                hash = AppendHash(hash, hash2);
                return hash;
            }
        }

        public static int Combine(int hash1, int hash2, int hash3) {
            unchecked {
                int hash = hash1;
                hash = AppendHash(hash, hash2);
                hash = AppendHash(hash, hash3);
                return hash;
            }
        }

        public static int Combine(int hash1, int hash2, int hash3, int hash4) {
            unchecked {
                int hash = hash1;
                hash = AppendHash(hash, hash2);
                hash = AppendHash(hash, hash3);
                hash = AppendHash(hash, hash4);
                return hash;
            }
        }

        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5) {
            unchecked {
                int hash = hash1;
                hash = AppendHash(hash, hash2);
                hash = AppendHash(hash, hash3);
                hash = AppendHash(hash, hash4);
                hash = AppendHash(hash, hash5);
                return hash;
            }
        }

        public static int Combine(in Span<int> hashes) {
            int hash = 0;
            unchecked {
                for (int i = 0; i < hashes.Length; i++) {
                    hash = AppendHash(hash, hashes[i]);
                }
            }
            return hash;
        }

        public static int Combine(in NativeArray<int> hashes) {
            int hash = 0;
            unchecked {
                for (int i = 0; i < hashes.Length; i++) {
                    hash = AppendHash(hash, hashes[i]);
                }
            }
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int AppendHash(int source, int next) {
            return (source * 397) + next;
        }
    }
}