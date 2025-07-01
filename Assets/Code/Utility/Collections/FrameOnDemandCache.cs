using System;
using System.Collections.Generic;
using UnityEngine;

namespace Awaken.Utility.Collections {
    /// <summary>
    /// Similar to <see cref="OnDemandCache{TKey,TValue}"/>, but the cache auto-invalidates itself after
    /// frames count returned by _frameClear function
    /// </summary>
    public class FrameOnDemandCache<TKey, TValue> {
        // === Fields
        int _frame;
        Dictionary<TKey, TValue> _mapping = new Dictionary<TKey, TValue>();
        Func<TKey, TValue> _factoryFunction;
        Func<int> _frameClear;

        // === Constructors

        public FrameOnDemandCache(Func<TKey, TValue> factoryFunction, Func<int> frameClear = null) {
            _factoryFunction = factoryFunction;
            _frameClear = frameClear ?? (static () => 1);
        }

        // === Indexing with on-demand creation
        public TValue this[TKey key] {
            get {
                CheckFrame();
                if (!_mapping.TryGetValue(key, out var value)) {
                    value = _factoryFunction(key);
                    _mapping[key] = value;
                }
                return value;
            }
            set {
                CheckFrame();
                _mapping[key] = value;
            }
        }

        void CheckFrame() {
            if (_frame <= Time.frameCount) {
                _mapping.Clear();
                _frame = Time.frameCount + _frameClear();
            }
        }

        // === Other methods (proxy to Dictionary)
        public bool Contains(TKey key) => _mapping.ContainsKey(key);
        public void Remove(TKey key) => _mapping.Remove(key);
        public void Clear() => _mapping.Clear();
        public IEnumerable<TKey> Keys => _mapping.Keys;
        public IEnumerable<TValue> Values => _mapping.Values;
        public int Count => _mapping.Count;

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _mapping.GetEnumerator();
    }
}
