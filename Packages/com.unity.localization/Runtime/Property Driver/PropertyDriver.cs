#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Localization.Bridge;

namespace UnityEngine.Localization
{
    internal abstract class PropertyDriver<T> : ScriptableSingleton<T> where T : ScriptableObject
    {
        public static void RegisterProperty(Object target, string propertyPath) => DrivenPropertyManagerBridge.RegisterProperty(instance, target, propertyPath);

        public static void UnregisterProperty(Object target, string propertyPath) => DrivenPropertyManagerBridge.UnregisterProperty(instance, target, propertyPath);

        public static void UnregisterProperties() => DrivenPropertyManagerBridge.UnregisterProperties(instance);

        void OnEnable() => name = GetType().Name;
    }
}
#endif
