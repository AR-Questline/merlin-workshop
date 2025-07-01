using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class QuaternionUtil {
        public static bool EqualsApproximately(this Quaternion quatA, Quaternion value, float acceptableRange) {
            return 1 - Mathf.Abs(Quaternion.Dot(quatA, value)) < acceptableRange;
        }
        
        public static bool IsValid(this Quaternion quat) {
            // check sum of squares in quaternion is 1
            return Mathf.Approximately(1, quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w);
        }
    }
}