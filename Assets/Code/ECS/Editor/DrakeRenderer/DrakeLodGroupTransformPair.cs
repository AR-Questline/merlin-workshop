using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public readonly struct DrakeLodGroupTransformPair : IEquatable<DrakeLodGroupTransformPair>, IEquatable<DrakeLodGroup> {
        public readonly DrakeLodGroup drakeLodGroup;
        public readonly Transform transform;

        public DrakeLodGroupTransformPair(DrakeLodGroup drakeLodGroup, Transform transform) {
            this.drakeLodGroup = drakeLodGroup;
            this.transform = transform;
        }

        public static implicit operator DrakeLodGroupTransformPair(DrakeLodGroup drake) {
            return new(drake, drake.transform);
        }

        public bool Equals(DrakeLodGroupTransformPair other) {
            return GetHashCode() == other.GetHashCode();
        }
        public bool Equals(DrakeLodGroup other) {
            return GetHashCode() == (other?.GetHashCode() ?? 0);
        }
        public override bool Equals(object obj) {
            return (obj is DrakeLodGroupTransformPair other && Equals(other)) ||
                   (obj is DrakeLodGroup drake && Equals(drake));
        }
        public override int GetHashCode() => drakeLodGroup.GetHashCode();

        public static bool operator ==(DrakeLodGroupTransformPair left, DrakeLodGroupTransformPair right) {
            return left.Equals(right);
        }
        public static bool operator ==(DrakeLodGroupTransformPair left, DrakeLodGroup right) {
            return left.Equals(right);
        }
        public static bool operator !=(DrakeLodGroupTransformPair left, DrakeLodGroupTransformPair right) {
            return !left.Equals(right);
        }
        public static bool operator !=(DrakeLodGroupTransformPair left, DrakeLodGroup right) {
            return !left.Equals(right);
        }
    }
}
