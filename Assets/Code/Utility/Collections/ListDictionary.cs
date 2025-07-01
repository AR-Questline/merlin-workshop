using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Utility.Collections {
    /// <summary>
    /// Implementation of dictionary based on arrays. Uses and linear search instead of hashtable.
    /// Stores key value pairs in two arrays (for keys and values).
    /// Useful where it is more important to allocate less memory than to have a O(1) query speed.
    /// Perfect for small dictionaries with 2-5 elements where O(n) is not much different from O(1). 
    /// </summary>
    [Serializable]
    public class ListDictionary<TKey, TValue> {
        const int DefaultCapacity = 5;

        UnsafePinnableList<TKey> _keys;
        EqualityComparer<TKey> _keyComparer;
        UnsafePinnableList<TValue> _values;
        EqualityComparer<TValue> _valueComparer;

        public int Capacity => _keys.Capacity;
        public int Count => _keys.Count;

        public TValue this[TKey key] {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public ListDictionary() : this(DefaultCapacity) { }

        public ListDictionary(int capacity, EqualityComparer<TKey> keyComparer = null, EqualityComparer<TValue> valueComparer = null) {
            _keys = new(capacity);
            _values = new(capacity);

            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public void EnsureCapacity(int capacity) {
            _keys.EnsureCapacity(capacity);
            _values.EnsureCapacity(capacity);
        }

        public bool TryAdd(in TKey key, in TValue value) {
            int index = IndexOfKey(key);
            if (index == -1) {
                _keys.Add(key);
                _values.Add(value);
                return true;
            }

            return false;
        }

        public int Add(in TKey key, in TValue value) {
            if (IndexOfKey(key) != -1) {
                throw new System.Exception($"Dictionary already contains key {key}");
            }

            int index = _keys.Count;
            _keys.Add(key);
            _values.Add(value);
            return index;
        }

        public void Clear() {
            _keys.Clear();
            _values.Clear();
        }

        public bool ContainsKey(in TKey key) {
            return IndexOfKey(key) != -1;
        }

        public bool ContainsValue(in TValue value) {
            return IndexOfValue(value) != -1;
        }

        public void SetValueAtIndex(int index, in TValue value) {
            _values[index] = value;
        }

        public TValue GetValueAtIndex(int index) {
            return _values[index];
        }

        public TKey GetKeyAtIndex(int index) {
            return _keys[index];
        }

        public KeyValuePair<TKey, TValue> GetKeyValuePairAtIndex(int index) {
            return new KeyValuePair<TKey, TValue>(_keys[index], _values[index]);
        }

        public List<KeyValuePair<TKey, TValue>> ToKeyValuePairList() {
            int count = Count;
            List<KeyValuePair<TKey, TValue>> newList = new(count);
            for (int i = 0; i < count; i++) {
                newList.Add(GetKeyValuePairAtIndex(i));
            }

            return newList;
        }

        public bool Remove(in TKey key) {
            int index = IndexOfKey(key);
            if (index == -1) {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        public ref TValue TryGetOrCreateValue(in TKey key, out int index, out bool created) {
            index = IndexOfKey(key);
            created = index == -1;
            if (created) {
                index = Add(key, default(TValue));
            }

            return ref GetValueByRefAtIndex(index);
        }

        public bool TryGetValue(in TKey key, out TValue value) {
            int index = IndexOfKey(key);
            if (index == -1) {
                value = default;
                return false;
            }

            value = _values[index];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) {
            if (index < 0 || index >= _keys.Count) {
                return;
            }

            _keys.SwapBackRemove(index);
            _values.SwapBackRemove(index);
        }

        /// <summary>
        /// Returns index of first key in internal List. This index becomes invalid after any Remove operation.
        /// </summary>
        /// <returns>Index of key if key exists, -1 otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfKey(in TKey searchedKey) => _keys.IndexOf(searchedKey, _keyComparer);

        /// <summary>
        /// Returns index of value in internal List. This index becomes invalid after the Remove operation.
        /// </summary>
        /// <returns>Index of first value if value exists, -1 otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfValue(in TValue searchedValue) => _values.IndexOf(searchedValue, _valueComparer);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            int arrayFreeElementsCount = array.Length - arrayIndex;
            if (arrayFreeElementsCount < _keys.Count) {
                throw new Exception(
                    $"Array with length {array.Length}, if starting from index {arrayIndex}, does not have capacity to hold {_keys.Count} elements");
            }

            for (int i = 0; i < _keys.Count; i++) {
                array[i + arrayIndex] = GetKeyValuePairAtIndex(i);
            }
        }

        public ref TValue GetValueByRef(in TKey key) {
            var index = IndexOfKey(key);
            if (index == -1) {
                throw new Exception($"Dictionary does not contain a key {key}");
            }

            return ref _values[index];
        }

        public ref TValue GetValueByRefAtIndex(int index) {
            if (index < 0 || index >= _keys.Count) {
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_keys.Count - 1}");
            }

            return ref _values[index];
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        TValue GetValue(in TKey key) {
            return GetValueByRef(key);
        }

        void SetValue(in TKey key, in TValue value) {
            int index = IndexOfKey(key);
            if (index == -1) {
                Add(key, value);
            } else {
                _values[index] = value;
            }
        }

        public struct Enumerator {
            readonly ListDictionary<TKey, TValue> _dictionary;
            int _index;

            public Enumerator(ListDictionary<TKey, TValue> dictionary) {
                _dictionary = dictionary;
                _index = -1;
            }

            public bool MoveNext() => ++_index < _dictionary.Count;

            public KeyValuePair<TKey, TValue> Current => new(_dictionary._keys[_index], _dictionary._values[_index]);
        }
    }
}