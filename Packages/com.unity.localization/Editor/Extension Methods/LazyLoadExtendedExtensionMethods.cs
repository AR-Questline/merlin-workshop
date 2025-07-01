using UnityEngine;

namespace UnityEditor.Localization
{
    static class LazyLoadExtendedExtensionMethods
    {
        public static int GetInstanceId<T>(this LazyLoadReference<T> lazy) where T : Object
        {
            #if UNITY_2020_1_OR_NEWER
            return lazy.instanceID;
            #else
            var field = lazy.GetType().GetField("m_InstanceID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (int)field.GetValue(lazy);
            #endif
        }
    }
}
