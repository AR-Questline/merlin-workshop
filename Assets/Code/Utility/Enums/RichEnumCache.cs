using System;
using System.Collections.Generic;
using System.Reflection;
using Awaken.Utility.Collections;

namespace Awaken.Utility.Enums {
    public static class RichEnumCache {
        public static T[] GetOnly<T>() => OnlyCache<T>.Values;
        public static T[] GetDerived<T>() => DerivedCache<T>.Values;

            
        static void FillWithFields<T>(List<T> list, Type type) {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            list.EnsureCapacity(list.Count + fields.Length);
            foreach (var field in fields) {
                if (field.FieldType == type) {
                    if (field.GetValue(null) is T value) {
                        list.Add(value);
                    }
                }
            }
        }
        
        class OnlyCache<T> {
            public static readonly T[] Values;
            
            static OnlyCache() {
                var baseType = typeof(T);
                var list = new List<T>();
                FillWithFields(list, baseType);
                Values = list.ToArray();
            }
        }

        class DerivedCache<T> {
            public static readonly T[] Values;
            
            static DerivedCache() {
                var baseType = typeof(T);
                var list = new List<T>();
                foreach (var type in baseType.Assembly.GetTypes()) {
                    if (baseType.IsAssignableFrom(type)) {
                        FillWithFields(list, type);
                    }
                }
                Values = list.ToArray();
            }
        }
    }
}