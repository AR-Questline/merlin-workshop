using System;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.IconRenderer {
    [Serializable]
    public struct TransformValues {
        public Vector3 position;
        public Vector3 rotation;
        public float scale;

        public TransformValues(Transform transform) {
            position = transform.position;
            rotation = transform.rotation.eulerAngles;
            scale = transform.localScale.x;
        }

        public static TransformValues Default => new() {
            position = Vector3.zero,
            rotation = Vector3.zero,
            scale = 1
        };

        public bool Equals(TransformValues other) {
            return position == other.position && rotation == other.rotation && scale == other.scale;
        }

        public override bool Equals(object obj) {
            return obj is TransformValues other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = position.GetHashCode();
                hashCode = (hashCode * 397) ^ rotation.GetHashCode();
                hashCode = (hashCode * 397) ^ scale.GetHashCode();
                return hashCode;
            }
        }

        public static TransformValues operator +(TransformValues left, TransformValues right) => new() {
            position = left.position + right.position, rotation = left.rotation + right.rotation, scale = left.scale + right.scale
        };
        public static bool operator ==(TransformValues left, TransformValues right) => left.Equals(right);
        public static bool operator !=(TransformValues left, TransformValues right) => !left.Equals(right);
    }

    public static class TransformValuesExtensions {
        public static void ApplyValues(this Transform transform, TransformValues values) {
            transform.position = values.position;
            transform.rotation = Quaternion.Euler(values.rotation);
            transform.localScale = Vector3.one * values.scale;
        }
    }
}