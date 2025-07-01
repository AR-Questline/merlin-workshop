using System.Collections.Generic;

namespace Awaken.Utility {
    static class StringKeyDictionaryExtensions {
        public static void SetValueLowerKey<T>(this Dictionary<string, T> dictionary, string key, T value) {
            dictionary[key.ToLowerInvariant()] = value;
        }
        
        public static bool TryGetValueLowerKey<T>(this Dictionary<string, T> dictionary, string key, out T value) {
            return dictionary.TryGetValue(key.ToLowerInvariant(), out value);
        }
        
        public static T GetValueOrDefaultLowerKey<T>(this Dictionary<string, T> dictionary, string key, T defaultValue) {
            return dictionary.GetValueOrDefault(key.ToLowerInvariant(), defaultValue);
        }
    }
}
