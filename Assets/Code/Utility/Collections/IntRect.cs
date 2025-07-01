using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Awaken.Utility.Collections {
    public struct IntRect {
        public int2 min;
        public int2 size;
        public IntRect(int2 min, int2 size) {
            this.min = min;
            this.size = size;
        }
        public readonly int2 Max => min + size;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Overlaps(IntRect other) {
            return math.all(other.min < Max) && math.all(other.Max > min); 
        }
        
        public readonly bool Contains(int2 position) {
            return math.all(position >= min) && math.all(position < Max);
        }
        public readonly bool ContainsRect(IntRect other) {
            return Contains(other.min) && Contains(other.Max);
        }
    }
}