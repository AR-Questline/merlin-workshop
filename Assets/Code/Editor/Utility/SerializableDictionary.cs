using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    [Serializable]
    public class SerializableDictionary<TK, TV> : Dictionary<TK, TV>, ISerializationCallbackReceiver {
        [SerializeField] List<TK> keys;
        [SerializeField] List<TV> values;
        
        public void OnBeforeSerialize() {
            keys = new List<TK>();
            values = new List<TV>();
            foreach (var data in this) {
                keys.Add(data.Key);
                values.Add(data.Value);
            }
        }

        public void OnAfterDeserialize() {
            for (int i = 0; i < keys.Count; i++) {
                if (values != null && values[i] != null) {
                    this[keys[i]] = values[i];
                }
            }
        }
    }
}