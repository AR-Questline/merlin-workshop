using UnityEngine;

namespace UnityEditor.Localization.Bridge
{
    internal class DrivenPropertyManagerInternalBridge
    {
        public static bool IsDriven(Object target, string propertyPath) => DrivenPropertyManagerInternal.IsDriven(target, propertyPath);
        public static bool IsDriving(Object driver, Object target, string propertyPath) => DrivenPropertyManagerInternal.IsDriving(driver, target, propertyPath);
    }
}
