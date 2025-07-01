using System;

namespace Awaken.Utility.Collections {
    public struct StructListDictionary<TKey, TValue> where TKey : IEquatable<TKey> {
        StructList<Pair> _pairs;

        public int Count => _pairs.Count;
        public StructListDictionary(int capacity) {
            _pairs = new(capacity);
        }

        public bool ContainsKey(in TKey key) {
            for (int i = 0; i < _pairs.Count; i++) {
                ref var pair = ref _pairs[i];
                if (pair.key.Equals(key)) {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetValue(in TKey key, out TValue value) {
            for (int i = 0; i < _pairs.Count; i++) {
                ref readonly var pair = ref _pairs[i];
                if (pair.key.Equals(key)) {
                    value = pair.value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public ref TValue this[int index] => ref _pairs[index].value;

        public ref TValue this[in TKey key] {
            get {
                for (int i = 0; i < _pairs.Count; i++) {
                    ref var pair = ref _pairs[i];
                    if (pair.key.Equals(key)) {
                        return ref pair.value;
                    }
                }
                throw new ArgumentException($"Key {key} not found");
            }
        }

        public bool TryGetIndex(in TKey key, out int index) {
            for (int i = 0; i < _pairs.Count; i++) {
                ref var pair = ref _pairs[i];
                if (pair.key.Equals(key)) {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public bool TryAdd(in TKey key, in TValue value) {
            if (ContainsKey(key)) {
                return false;
            }
            _pairs.Add(new Pair(key, value));
            return true;
        }

        public void Add(in TKey key, in TValue value) {
            if (ContainsKey(key)) {
                throw new Exception($"Dictionary already contains key {key}");
            }
            _pairs.Add(new Pair(key, value));
        }

        public bool Remove(in TKey key) {
            if (TryGetIndex(key, out var index)) {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index) {
            _pairs.RemoveAtSwapBack(index);
        }

        public void Clear() {
            _pairs.Clear();
        }

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