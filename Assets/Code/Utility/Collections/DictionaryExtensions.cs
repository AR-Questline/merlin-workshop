using System.Collections.Generic;

namespace Awaken.Utility
{
    public static class DictionaryExtensions {
        public static Dictionary<TValue, TKey> InvertDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) {
            var invertedDictionary = new Dictionary<TValue, TKey>(dictionary.Count);
            foreach (var (key, value) in dictionary) {
                invertedDictionary.Add(value, key);
            }
            return invertedDictionary;
        }
    }
}
