using System;

namespace Awaken.TG.Main.General {
    [Serializable]
    public struct ConditionalInt : IEquatable<ConditionalInt> {
        public bool enable;
        public int value;

        public ConditionalInt(bool enable, int value) {
            this.enable = enable;
            this.value = value;
        }

        public ConditionalInt(int value) : this() {
            enable = true;
            this.value = value;
        }

        public static implicit operator bool(ConditionalInt conditionalInt) => conditionalInt.enable;
        public static implicit operator int?(ConditionalInt conditionalInt) => conditionalInt.enable ? (int?)conditionalInt.value : null;
        public static implicit operator ConditionalInt(int? nullableInt) => nullableInt.HasValue ? new ConditionalInt(nullableInt.Value) : new ConditionalInt();
        public static explicit operator int(ConditionalInt conditionalInt) => conditionalInt.value;
        public static explicit operator ConditionalInt(int intValue) => new ConditionalInt(intValue);

        public override string ToString() {
            return enable ? value.ToString() : enable.ToString();
        }
        
        public bool Equals(ConditionalInt other) {
            return enable == other.enable && (!enable || value == other.value);
        }

        public override bool Equals(object obj) {
            return obj is ConditionalInt other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (enable.GetHashCode() * 397) ^ (enable ? value : 1);
            }
        }

        public static bool operator ==(ConditionalInt left, ConditionalInt right) {
            return left.Equals(right);
        }

        public static bool operator !=(ConditionalInt left, ConditionalInt right) {
            return !left.Equals(right);
        }
    }
}