using System;
using UnityEngine;

namespace Awaken.Utility {
    public struct GizmosColor : IDisposable {
        Color _previousColor;

        public GizmosColor(Color color) {
            _previousColor = Gizmos.color;
            Gizmos.color = color;
        }

        public void Dispose() {
            Gizmos.color = _previousColor;
        }
    }
}