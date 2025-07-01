using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC {
    [Il2CppEagerStaticClassConstruction]
    public static class TypeNameCache {
        static Dictionary<Type, string> s_nameByType = new();
        static Dictionary<Type, string> s_qualifiedNameByType = new();
        
        public static string Name(Type t) {
            if (!s_nameByType.TryGetValue(t, out var name)) {
                name = t.Name;
                s_nameByType.Add(t, name);
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
    }
}
