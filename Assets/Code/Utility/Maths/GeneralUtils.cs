using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class GeneralUtils {
        public static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
        
        public static float ClampEulerAngle(float angle, float from, float to) {
            if (angle < 0f) angle = 360 + angle;
            if (angle > 180f) return Mathf.Max(angle, 360+from);
            return Mathf.Min(angle, to);
        }
    }
}
