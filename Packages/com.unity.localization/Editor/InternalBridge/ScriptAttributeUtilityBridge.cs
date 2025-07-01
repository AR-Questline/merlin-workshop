using System;
using System.Reflection;

namespace UnityEditor.Localization.Bridge
{
    static partial class ScriptAttributeUtilityBridge
    {
        public static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type) => ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty(property, out type);

        public static bool GetTypeFromManagedReferenceFullTypeName(string managedReferenceFullTypename, out Type managedReferenceInstanceType) => ScriptAttributeUtility.GetTypeFromManagedReferenceFullTypeName(managedReferenceFullTypename, out managedReferenceInstanceType);
    }
}
