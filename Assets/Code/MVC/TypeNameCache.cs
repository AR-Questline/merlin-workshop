using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility.SerializableTypeReference {
    [Il2CppEagerStaticClassConstruction]
    public static class TypeNameCache {
        static Dictionary<Type, string> s_nameByType = new();
        static Dictionary<Type, string> s_niceNameByType = new();
        static Dictionary<Type, string> s_qualifiedNameByType = new();
        static Dictionary<Type, string> s_serializableNameByType = new();
        
        public static string Name(Type t) {
            if (!s_nameByType.TryGetValue(t, out var name)) {
                name = t.Name;
                s_nameByType.Add(t, name);
            }
            return name;
        }
        
        public static string NiceName(Type t) {
            if (!s_niceNameByType.TryGetValue(t, out var name)) {
                name = StringUtil.NicifyName(t.Name);
                s_niceNameByType.Add(t, name);
            }
            return name;
        }
        
        public static string QualifiedName(Type t) {
            if (!s_qualifiedNameByType.TryGetValue(t, out var qualifiedName)) {
                qualifiedName = t.AssemblyQualifiedName;
                if (qualifiedName == null) {
                    Log.Important?.Error($"Null assembly qualified name for type: {t}");
                    qualifiedName = string.Empty;
                }
                s_qualifiedNameByType.Add(t, qualifiedName);
            }
            return qualifiedName;
        }
        
        public static string SerializableName(Type t) {
            if (t == null) {
                return "";
            }
            if (!s_serializableNameByType.TryGetValue(t, out var serializableName)) {
                serializableName = t.FullName + ", " + t.Assembly.GetName().Name;
                s_serializableNameByType.Add(t, serializableName);
            }
            return serializableName;
        }
    }
}
