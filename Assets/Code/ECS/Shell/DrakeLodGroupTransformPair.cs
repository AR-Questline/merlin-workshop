using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public readonly struct DrakeLodGroupTransformPair : IEquatable<DrakeLodGroupTransformPair>,
        IEquatable<DrakeLodGroup> {
        public readonly DrakeLodGroup drakeLodGroup;
        public readonly Transform transform;

        public DrakeLodGroupTransformPair(DrakeLodGroup drakeLodGroup, Transform transform) {
            throw new NotImplementedException();
        }

        public static implicit operator DrakeLodGroupTransformPair(DrakeLodGroup drake) {
            throw new NotImplementedException();
        }

        public bool Equals(DrakeLodGroupTransformPair other) {
            throw new NotImplementedException();
        }

        public bool Equals(DrakeLodGroup other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() => throw new NotImplementedException();

        public static bool operator ==(DrakeLodGroupTransformPair left, DrakeLodGroupTransformPair right) {
            throw new NotImplementedException();
        }

        public static bool operator ==(DrakeLodGroupTransformPair left, DrakeLodGroup right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(DrakeLodGroupTransformPair left, DrakeLodGroupTransformPair right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(DrakeLodGroupTransformPair left, DrakeLodGroup right) {
            throw new NotImplementedException();
        }
    }
}