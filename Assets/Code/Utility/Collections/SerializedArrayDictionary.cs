using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Collections {
    [Serializable]
    public struct SerializedArrayDictionary<TKey, TValue> where TKey : IEquatable<TKey> {
        public static SerializedArrayDictionary<TKey, TValue> Empty => new() { pairs = Array.Empty<Pair>() };
        
        [SerializeField, TableList(AlwaysExpanded = true), HideLabel] Pair[] pairs;
        
        public bool IsCreated => pairs != null;
        
        public bool ContainsKey(in TKey key) {
            for (int i = 0; i < pairs.Length; i++) {
                ref var pair = ref pairs[i];
                if (pair.key.Equals(key)) {
                    return true;
                }
            }
            return false;
        }
        
        public readonly bool TryGetValue(in TKey key, out TValue value) {
            for (int i = 0; i < pairs.Length; i++) {
                ref readonly var pair = ref pairs[i];
                if (pair.key.Equals(key)) {
                    value = pair.value;
                    return true;
                }
            }
            value = default;
            return false;
        }
        
        public ref TValue this[int index] => ref pairs[index].value;
        
        public ref TValue this[in TKey key] { get {
            for (int i = 0; i < pairs.Length; i++) {
                ref var pair = ref pairs[i];
                if (pair.key.Equals(key)) {
                    return ref pair.value;
                }
            }
            throw new ArgumentException($"Key {key} not found");
        }}

        public bool TryGetIndex(in TKey key, out int index) {
            for (int i = 0; i < pairs.Length; i++) {
                ref var pair = ref pairs[i];
                if (pair.key.Equals(key)) {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
        
        [Serializable]
        struct Pair {
            public TKey key;
            public TValue value;
            
            public Pair(in TKey key, in TValue value) {
                this.key = key;
                this.value = value;
            }
            
            public void Deconstruct(out TKey key, out TValue value) {
                key = this.key;
                value = this.value;
            }
        }
        
#if UNITY_EDITOR
        public struct EditorAccessor {
            public bool TryAdd(ref SerializedArrayDictionary<TKey, TValue> dictionary, in TKey key, in TValue value) {
                if (dictionary.ContainsKey(key)) {
                    return false;
                }
                ArrayUtils.Add(ref dictionary.pairs, new Pair(key, value));
                return true;
            }

            public void Add(ref SerializedArrayDictionary<TKey, TValue> dictionary, in TKey key, in TValue value) {
                if (dictionary.ContainsKey(key)) {
                    throw new Exception($"Dictionary already contains key {key}");
                }
                ArrayUtils.Add(ref dictionary.pairs, new Pair(key, value));
            }

            public bool Remove(ref SerializedArrayDictionary<TKey, TValue> dictionary, in TKey key) {
                if (dictionary.TryGetIndex(key, out var index)) {
                    ArrayUtils.RemoveAt(ref dictionary.pairs, index);
                    return true;
                }
                return false;
            }
        
            public void RemoveAt(ref SerializedArrayDictionary<TKey, TValue> dictionary, int index) {
                ArrayUtils.RemoveAt(ref dictionary.pairs, index);
            }
        }
#endif
    }
}