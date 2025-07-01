using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.Utility.Collections {
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
        [SerializeField] List<Pair> pairs = new();

        public void OnBeforeSerialize() {
            pairs.Clear();
            foreach (var kvp in this) {
                pairs.Add(new Pair(kvp.Key, kvp.Value));
            }
        }
        
        public void OnAfterDeserialize() {
            Clear();
            foreach (var pair in pairs) {
                TryAdd(pair.key, pair.value);
            }
            pairs.Clear();
        }

        [Serializable]
        struct Pair {
            public TKey key;
            public TValue value;
            
            public Pair(TKey key, TValue value) {
                this.key = key;
                this.value = value;
            }
        }
    }
}