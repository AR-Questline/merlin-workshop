using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class DrawPositionHistory : MonoBehaviour {
        public float colliderWidth = 0.35f, colliderHeight = 1.9f;
        readonly List<Matrix4x4> _cubesToDraw = new();
        bool _record;

        [Button]
        public void StartRecording() {
            _record = true;
        }

        [Button]
        public void StopRecording() {
            _record = false;
        }

        [Button]
        public void ClearRecording() {
            _cubesToDraw.Clear();
        }

        void Update() {
            if (_record) {
                var matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                _cubesToDraw.Add(matrix);
            }
        }

        void OnDrawGizmos() {
            foreach (var matrix in _cubesToDraw) {
                Gizmos.matrix = matrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(colliderWidth, colliderHeight, colliderWidth));
            }
        }
    }
}
