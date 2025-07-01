using UnityEngine;

namespace Awaken.TG.Utility {
    public static class GizmosUtil {
        /// <summary>
        /// Draws 3D gizmos cross.
        /// </summary>
        public static void DrawCross3D(Vector3 point, float size) {
            for (int i = 0; i < 3; i++) {
                var offset = Vector3.zero;
                offset[i] = size;
                Gizmos.DrawLine(point - offset, point + offset);
            }
        }
    }
}