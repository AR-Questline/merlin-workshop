using Awaken.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Assets.Utility.Collections {
    /// <summary>
    /// Serializable multi-type dictionary
    /// </summary>
    public partial class MultiTypeDictionary<T> {
        public ushort TypeForSerialization => SavedTypes.MultiTypeDictionary;

        public readonly Dictionary<T, object> dictionary;
        
        public MultiTypeDictionary() {
            dictionary = new Dictionary<T, object>();
        }

        public MultiTypeDictionary(Dictionary<T, object> dictionary) {
            this.dictionary = dictionary;
        }

        public TValue Get<TValue>(T key, TValue defaultValue = default, bool writeOnDefault = false) {
            object result = Get(key);

            if (result == null) {
                if (writeOnDefault) {
                    Set(key, defaultValue);
                }
                return defaultValue;
            }
            if (result is TValue castedResult) return castedResult;

            if (typeof(TValue).IsEnum) {
                int intValue = (int) Convert.ChangeType(result, typeof(int));
                return (TValue) (object) intValue;
            }
            
            // If we get a not-null result and it's not of proper type, try converting it (only way to cast System.Int64 to System.Int32)
            object converted = Convert.ChangeType(result, typeof(TValue));
            if (converted == null) {
                throw new ArgumentException($"Requested value is of other type. Requested: {typeof(TValue)}, Current: {result.GetType()}");
            }

            return (TValue) converted;
        }

        public object Get(T key) {
            if (dictionary.ContainsKey(key)) {
                return dictionary[key];
            }

            return null;
        }

        public void Set(T key, object val) {
            dictionary[key] = val;
        }

        public bool HasValue<TVal>(T key) {
            object result = Get(key);

            if (result == null) {
                return false;
            }

            if (result is TVal) {
                return true;
            }

            return false;
        }

        public bool HasValue(T key) {
            return Get(key) != null;
        }
        
        public bool Remove(T key) {
            return dictionary.Remove(key);
        }

        public void Clear() {
            dictionary.Clear();
        }

        public bool IsEmpty => dictionary.Keys.Count == 0;
        public IEnumerable<KeyValuePair<T, object>> KeyValues => dictionary;
    }
}