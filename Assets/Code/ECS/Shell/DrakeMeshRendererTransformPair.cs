using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public readonly struct DrakeMeshRendererTransformPair : IEquatable<DrakeMeshRendererTransformPair>,
        IEquatable<DrakeMeshRenderer> {
        public readonly DrakeMeshRenderer drakeMeshRenderer;
        public readonly Transform transform;

        public DrakeMeshRendererTransformPair(DrakeMeshRenderer drakeMeshRenderer, Transform transform) {
            throw new NotImplementedException();
        }

        public static implicit operator DrakeMeshRendererTransformPair(DrakeMeshRenderer drake) {
            throw new NotImplementedException();
        }

        public bool Equals(DrakeMeshRendererTransformPair other) {
            throw new NotImplementedException();
        }

        public bool Equals(DrakeMeshRenderer other) {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj) {
            throw new NotImplementedException();
        }

        public override int GetHashCode() {
            throw new NotImplementedException();
        }

        public static bool operator ==(DrakeMeshRendererTransformPair left, DrakeMeshRendererTransformPair right) {
            throw new NotImplementedException();
        }

        public static bool operator ==(DrakeMeshRendererTransformPair left, DrakeMeshRenderer right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(DrakeMeshRendererTransformPair left, DrakeMeshRendererTransformPair right) {
            throw new NotImplementedException();
        }

        public static bool operator !=(DrakeMeshRendererTransformPair left, DrakeMeshRenderer right) {
            throw new NotImplementedException();
        }
    }
}