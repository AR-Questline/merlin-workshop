using System;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility {
    [Serializable, Il2CppEagerStaticClassConstruction]
    public struct BlittableBool : IEquatable<BlittableBool> {
        [UnityEngine.Scripting.Preserve] public static readonly BlittableBool False = new() { state = 0 };
        [UnityEngine.Scripting.Preserve] public static readonly BlittableBool True = new() { state = 1 };

        public byte state;

        public static implicit operator bool(BlittableBool value) {
            return value.state != 0;
        }

        public static implicit operator BlittableBool(bool value) {
            return new() { state = (byte)(value ? 1 : 0) };
        }

        public bool Equals(BlittableBool other) {
            return state == other.state;
        }
        public override bool Equals(object obj) {
            return obj is BlittableBool other && Equals(other);
        }
        public override int GetHashCode() {
            return state.GetHashCode();
        }
        public static bool operator ==(BlittableBool left, BlittableBool right) {
            return left.Equals(right);
        }
        public static bool operator !=(BlittableBool left, BlittableBool right) {
            return !left.Equals(right);
        }
    }
}
