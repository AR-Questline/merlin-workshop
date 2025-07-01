using System;
using System.Collections.Generic;
using System.Text;
using Awaken.TG.Assets.Utility.Collections;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Memories {
    /// <summary>
    /// Container for all contextual facts that happened for given selector.
    /// Selector determines all objects that participated 
    /// </summary>
    public partial class ContextualFacts {
        public ushort TypeForSerialization => SavedTypes.ContextualFacts;

        // === References
        [Saved] public StringCollectionSelector Selector { get; private set; }
        [Saved] MultiTypeDictionary<string> _values = new MultiTypeDictionary<string>();
        [Saved] MultiMap<string, string> _keysByOwner = new MultiMap<string, string>();

        public bool IsEmpty => _values.IsEmpty;

        // === Constructor
        public ContextualFacts(StringCollectionSelector selector) {
            Selector = selector;
        }

        // === Public interface
        public T Get<T>(string label) => Get(label, default(T));
        public T Get<T>(string label, T defaultValue) => _values.Get(label, defaultValue);
        [UnityEngine.Scripting.Preserve] 
        public T Get<T>(string label, T defaultValue, bool writeOnDefault) => _values.Get(label, defaultValue, writeOnDefault);

        public object Get(string label) {
            return _values.Get(label);
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll() => _values.KeyValues;

        public void Set<T>(string label, T val, IModel[] owners = null) {
    #if UNITY_EDITOR
            if (val is not (null or bool or int or float or string or QuestState or ObjectiveState)) {
                throw new Exception($"Trying to set unsupported type in ContextualFacts: {val.GetType()}\n" + 
                    "If you truly need to store this type you need to add its support in SaveWriter.ReadMultiTypeDictionary and SaveReader.ReadMultiTypeDictionary");
            }
    #endif
            _values.Set(label, val);
            if (owners != null) {
                foreach (var owner in owners.WhereNotNull()) {
                    _keysByOwner.Add(owner.ID, label);
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        public T Change<T>(string label, Func<T, T> func, T defaultValue = default(T)) {
            T oldValue = _values.Get(label, defaultValue);
            T newValue = func(oldValue);
            _values.Set(label, newValue);
            return newValue;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void Remove(string label) {
            _values.Remove(label);
            foreach (var keyValuePair in _keysByOwner) {
                if (keyValuePair.Value.Contains(label)) {
                    keyValuePair.Value.Remove(label);
                }
            }
        }
        
        public void Merge(ContextualFacts other) {
            foreach (var kvp in other._values.KeyValues) {
                if (!_values.HasValue(kvp.Key)) {
                    _values.Set(kvp.Key, kvp.Value);
                }
            }
        }

        public bool HasValue(string label) => _values.HasValue(label);
        [UnityEngine.Scripting.Preserve] public bool HasValue<TVal>(string label) => _values.HasValue<TVal>(label);

        public void Clear() {
            _values.Clear();
            _keysByOwner.Clear();
        }
        
        // === ToString
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (var value in _values.KeyValues) {
                sb.Append("(");
                sb.Append(value.Key);
                sb.Append(": ");
                sb.Append(value.Value);
                sb.Append(") ");
            }
            return sb.ToString();
        }

        public void OnModelDiscarded(IModel model) {
            if(_keysByOwner.TryGetValue(model.ID, out HashSet<string> keys)) {
                foreach (string key in keys) {
                    _values.Remove(key);
                }
            }
            _keysByOwner.RemoveAll(model.ID);
        }
    }
}