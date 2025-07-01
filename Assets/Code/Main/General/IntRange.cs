using System;
using Awaken.TG.Code.Utility;

namespace Awaken.TG.Main.General
{
    /// <summary>
    /// Represents an integer range with a low end and a high end, both inclusive.
    /// </summary>
    [Serializable]
    public struct IntRange : IEquatable<IntRange> {
        // === Properties

        public int low, high;

        // === Constructors

        public IntRange(int low, int high) {
            if (low > high) {
                (low, high) = (high, low);
            }
            this.low = low;
            this.high = high;
        }

        // === Operations
        public readonly bool Contains(int number) => number >= low && number <= high;
        public readonly bool Contains(float number) => number >= low && number <= high;

        public readonly int RandomPick() {
            return UnityEngine.Random.Range(low, high + 1);
        }
        public readonly float Average() => (low + high) / 2f;
        public readonly int RogueRandomPick() => RandomUtil.UniformInt(low, high);

        // === Conversions
        public readonly override string ToString() => $"{low}..{high}";
        
        // === Operators
        public static IntRange operator +(IntRange a, IntRange b) {
            return new IntRange(a.low + b.low, a.high + b.high);
        }

        public static IntRange operator -(IntRange a, IntRange b) {
            return new IntRange(a.low - b.low, a.high - b.high);
        }

        // === Equality
        public bool Equals(IntRange other) {
            return low == other.low && high == other.high;
        }
        public override bool Equals(object obj) {
            return obj is IntRange other && Equals(other);
        }
        public override int GetHashCode() {
            unchecked {
                return (low * 397) ^ high;
            }
        }
        public static bool operator ==(IntRange left, IntRange right) {
            return left.Equals(right);
        }
        public static bool operator !=(IntRange left, IntRange right) {
            return !left.Equals(right);
        }
    }
}
