using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public readonly struct DrakeMeshRendererTransformPair : IEquatable<DrakeMeshRendererTransformPair>, IEquatable<DrakeMeshRenderer> {
        public readonly DrakeMeshRenderer drakeMeshRenderer;
        public readonly Transform transform;

        public DrakeMeshRendererTransformPair(DrakeMeshRenderer drakeMeshRenderer, Transform transform) {
            this.drakeMeshRenderer = drakeMeshRenderer;
            this.transform = transform;
        }

        public static implicit operator DrakeMeshRendererTransformPair(DrakeMeshRenderer drake) {
            return new(drake, drake.transform);
        }

        public bool Equals(DrakeMeshRendererTransformPair other) {
            return GetHashCode() == other.GetHashCode();
        }
        public bool Equals(DrakeMeshRenderer other) {
            return GetHashCode() == (other?.GetHashCode() ?? 0);
        }
        public override bool Equals(object obj) {
            return (obj is DrakeMeshRendererTransformPair other && Equals(other)) ||
                   (obj is DrakeMeshRenderer drake && Equals(drake));
        }
        public override int GetHashCode() {
            return drakeMeshRenderer != null ? drakeMeshRenderer.GetHashCode() : 0;
        }
        public static bool operator ==(DrakeMeshRendererTransformPair left, DrakeMeshRendererTransformPair right) {
            return left.Equals(right);
        }
        public static bool operator ==(DrakeMeshRendererTransformPair left, DrakeMeshRenderer right) {
            return left.Equals(right);
        }
        public static bool operator !=(DrakeMeshRendererTransformPair left, DrakeMeshRendererTransformPair right) {
            return !left.Equals(right);
        }
        public static bool operator !=(DrakeMeshRendererTransformPair left, DrakeMeshRenderer right) {
            return !left.Equals(right);
        }
    }
}
