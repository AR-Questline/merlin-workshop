using UnityEngine;

namespace Awaken.Utility {
    public readonly ref struct GizmosMatrix {
        readonly Matrix4x4 _previousMatrix;

        public GizmosMatrix(Matrix4x4 matrix) {
            _previousMatrix = Gizmos.matrix;
            Gizmos.matrix = matrix;
        }

        [UnityEngine.Scripting.Preserve]
        public void Dispose() {
            Gizmos.matrix = _previousMatrix;
        }
    }
}