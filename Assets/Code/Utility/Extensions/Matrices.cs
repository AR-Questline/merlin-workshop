using System.Runtime.CompilerServices;
using UnityEngine;

namespace Awaken.Utility.Extensions {
    public static class Matrices {
        [UnityEngine.Scripting.Preserve]
        public static void SetFromMatrix(this Transform transform, Matrix4x4 matrix) {
            transform.localScale = matrix.ExtractScale();
            transform.localRotation = matrix.ExtractRotation();
            transform.localPosition = matrix.ExtractPosition();
        }

        public static void SetFromWorldSpaceMatrix(this Transform transform, Matrix4x4 matrix, bool useLocalScale = true) {
            Vector3 scale = matrix.ExtractScale();
            if (useLocalScale) {
                transform.localScale = new Vector3(scale.x / transform.localScale.x, scale.y / transform.localScale.y, scale.z / transform.localScale.z);
            }
            else {
                transform.localScale = Vector3.one;
                transform.localScale = new Vector3(scale.x / transform.lossyScale.x, scale.y / transform.lossyScale.y, scale.z / transform.lossyScale.z);
            }
            transform.rotation = matrix.ExtractRotation();
            transform.position = matrix.ExtractPosition();
        }

        public static Matrix4x4 LocalTransformToMatrix(this Transform transform) {
            transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            return Matrix4x4.TRS(localPosition, localRotation, transform.localScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ExtractRotation(this Matrix4x4 matrix) {
            return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ExtractPosition(this Matrix4x4 matrix) {
            return matrix.GetColumn(3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ExtractScale(this Matrix4x4 matrix) {
            return new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude
            );
        }
    }
}