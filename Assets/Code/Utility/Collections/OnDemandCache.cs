using System;
using System.Collections.Generic;

namespace Awaken.Utility.Collections
{
    /// <summary>
    /// A cache is a simple dictionary that knows how to create elements on demand.
    /// Encapsulate the 'if (!cache.ContainsKey(k)) { create it then cache and return it }" pattern.
    /// </summary>
    public class OnDemandCache<TKey, TValue>
    {
        // === Fields

        Dictionary<TKey, TValue> _mapping = new Dictionary<TKey, TValue>();
        Func<TKey, TValue> _factoryFunction;

        // === Constructors

        public OnDemandCache(Func<TKey, TValue> factoryFunction) {
            _factoryFunction = factoryFunction;
        }

        // === Indexing with on-demand creation

        public TValue this[TKey key] {
            get {
                if (!_mapping.TryGetValue(key, out var value)) {
                    value = _factoryFunction(key);
                    _mapping[key] = value;
                }
                return value;
            }
            set => _mapping[key] = value;
        }

        public void Generate(TKey key) {
            var _ = this[key];
        }

        public void Reset(TKey key) {
            _mapping[key] = _factoryFunction(key);
        }

        // === Other methods (proxy to Dictionary)

        public bool Contains(TKey key) => _mapping.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _mapping.TryGetValue(key, out value);
        public void Remove(TKey key) => _mapping.Remove(key);
        public void Clear() => _mapping.Clear();
        public IEnumerable<TKey> Keys => _mapping.Keys;
        public IEnumerable<TValue> Values => _mapping.Values;
        public int Count => _mapping.Count;

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _mapping.GetEnumerator();
    }
}
