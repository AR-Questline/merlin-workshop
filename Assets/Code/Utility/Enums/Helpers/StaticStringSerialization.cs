using System;
using System.Reflection;
using System.Text;
using Awaken.Utility.Collections;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility.Enums.Helpers {
    /// <summary>
    /// A helper that can handle serialization to/from string for any "enum-like" class
    /// that stores its instance in 'public static readonly' fields.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public static class StaticStringSerialization {
        // === Cache
        static OnDemandCache<string, Type> s_typeCache = new(Type.GetType);
        static OnDemandCache<string, object> s_instanceCache = new(ResolveSerializedString);
        static OnDemandCache<Type, string> s_qualifiedNameByType = new(t => t.AssemblyQualifiedName);
        static StringBuilder s_stringBuilder = new(1024);

        // === Public API
        public static string TypeName(Type type) {
            return s_qualifiedNameByType[type];
        }
        
        public static string Serialize(Type owningType, string instanceName) {
            s_stringBuilder.Append(s_qualifiedNameByType[owningType]);
            s_stringBuilder.Append(':');
            s_stringBuilder.Append(instanceName);
            var result = s_stringBuilder.ToString();
            s_stringBuilder.Clear();
            return result;
        }

        public static T Deserialize<T>(string serializedString) where T : class {
            return s_instanceCache[serializedString] as T;
        }

        // === Low-level resolution
        static object ResolveSerializedString(string serializedString) {
            string[] elements = serializedString.Split(':');
            if (elements.Length != 2) throw new InvalidOperationException($"Serialized string is not of the correct format: '{serializedString}'");
            string typeName = elements[0], instanceName = elements[1];
            Type type = s_typeCache[typeName];
            if (type == null) throw new InvalidOperationException($"Unknown type: {typeName}");
            FieldInfo instanceField = type.GetField(instanceName, BindingFlags.Public | BindingFlags.Static);
            if (instanceField == null) throw new InvalidOperationException($"Static field '{instanceName}' does not exist in {serializedString}");
            return instanceField.GetValue(null);
        }
    }
}
