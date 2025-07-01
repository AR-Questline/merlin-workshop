using System;
using System.Collections.Generic;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.CodeClarity;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Collections {
    [Serializable]
    [DoNotUseDefaultConstructor]
    public partial struct StructList<T> {
        public ushort TypeForSerialization => SavedTypes.StructList;

        const int DefaultCapacity = 4;

        static readonly T[] EmptyArray = Array.Empty<T>();
        public static readonly StructList<T> Empty = new(0);

        [SerializeField, ReadOnly] int _count;
        [SerializeField]
#if UNITY_EDITOR
        [ListDrawerSettings(OnBeginListElementGUI = nameof(BeginDrawListElement), OnEndListElementGUI = nameof(EndDrawListElement))]
        [OnValueChanged(nameof(OnItemsArrayValueChanged))]
#endif
        T[] _items;

        public readonly int Count => _count;
        public int Capacity {
            readonly get => _items.Length;
            set {
                if (value == _items.Length) {
                    return;
                }
                if (value > 0) {
                    T[] destinationArray = new T[value];
                    if (_count > 0) {
                        Array.Copy(_items, 0, destinationArray, 0, _count);
                    }
                    _items = destinationArray;
                } else {
                    _items = EmptyArray;
                }
            }
        }

        public readonly bool IsCreated => _items != null;
        public readonly T[] BackingArray => _items;

        public StructList(int capacity) {
            _items = capacity <= 0 ? EmptyArray : new T[capacity];
            _count = 0;
        }

        public StructList(T[] backingArray) {
            _items = backingArray;
            _count = backingArray.Length;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public ref T this[int index] {
            get {
                Asserts.IndexInRange(index, _count);
                return ref _items[index];
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public ref T this[uint index] {
            get {
                Asserts.IndexInRange(index, _count);
                return ref _items[index];
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Add(T item) {
            if (_count == _items.Length) {
                EnsureCapacity(_count + 1);
            }
            _items[_count++] = item;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public bool AddUnique(T item) {
            var index = IndexOf(item);
            if (index == -1) {
                Add(item);
                return true;
            } else {
                return false;
            }
        }

        public readonly int IndexOf(T item) {
            return Array.IndexOf(_items, item, 0, _count);
        }

        public readonly int IndexOf(T item, int index) {
            return Array.IndexOf(_items, item, index, _count - index);
        }

        public readonly int IndexOf(T item, EqualityComparer<T> comparer) {
            for (int i = 0; i < _count; i++) {
                if (comparer.Equals(item, _items[i])) {
                    return i;
                }
            }
            return -1;
        }

        public readonly int IndexOf(T item, int index, int count) {
            return Array.IndexOf(_items, item, index, count);
        }

        public readonly bool Contains(T item) {
            EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            return Contains(item, equalityComparer);
        }

        public readonly bool Contains(T item, EqualityComparer<T> equalityComparer) {
            for (int index = 0; index < _count; ++index) {
                if (equalityComparer.Equals(_items[index], item)) {
                    return true;
                }
            }
            return false;
        }

        public void Insert(int index, T item) {
            if (_count == _items.Length) {
                EnsureCapacity(_count + 1);
            }
            if (index < _count) {
                Array.Copy(_items, index, _items, index + 1, _count - index);
            }
            _items[index] = item;
            ++_count;
        }

        public void InsertRange(int index, IEnumerable<T> collection) {
            if (collection is ICollection<T> objs) {
                int count = objs.Count;
                if (count > 0) {
                    EnsureCapacity(_count + count);
                    if (index < _count) {
                        Array.Copy(_items, index, _items, index + count, _count - index);
                    }
                    objs.CopyTo(_items, index);
                    _count += count;
                }
            } else {
                foreach (T obj in collection) {
                    Insert(index++, obj);
                }
            }
        }

        public bool Remove(T item) {
            int index = IndexOf(item);
            if (index < 0) {
                return false;
            }
            RemoveAt(index);
            return true;
        }
        
        public bool RemoveSwapBack(T item) {
            int index = IndexOf(item);
            if (index < 0) {
                return false;
            }
            RemoveAtSwapBack(index);
            return true;
        }

        public int RemoveAll(Predicate<T> match) {
            int index1 = 0;
            while (index1 < _count && !match(_items[index1])) {
                ++index1;
            }
            if (index1 >= _count) {
                return 0;
            }
            int index2 = index1 + 1;
            while (index2 < _count) {
                while (index2 < _count && match(_items[index2])) {
                    ++index2;
                }
                if (index2 < _count) {
                    _items[index1++] = _items[index2++];
                }
            }
            Array.Clear((Array)_items, index1, _count - index1);
            int num = _count - index1;
            _count = index1;
            return num;
        }

        public void RemoveAt(int index) {
            --_count;
            if (index < _count) {
                Array.Copy(_items, index + 1, _items, index, _count - index);
            }
            _items[_count] = default;
        }

        public void RemoveRange(int index, int count) {
            if (count <= 0) {
                return;
            }
            int size = _count;
            _count -= count;
            if (index < _count) {
                Array.Copy(_items, index + count, _items, index, _count - index);
            }
            Array.Clear(_items, _count, count);
        }

        public void RemoveAtSwapBack(int index) {
            var lastIndex = _count - 1;
            _items[index] = _items[lastIndex];
            _items[lastIndex] = default;
            --_count;
        }

        public void EnsureCapacity(int min) {
            if (_items.Length >= min) {
                return;
            }

            int num = math.max(DefaultCapacity, _items.Length * 2);
            if ((uint)num > 2146435071U) {
                num = 2146435071;
            }
            if (num < min) {
                num = min;
            }
            Capacity = num;
        }

        public void EnsureCapacityExact(int min) {
            if (_items.Length >= min) {
                return;
            }

            Capacity = min;
        }

        public void Clear() {
            if (_count > 0) {
                Array.Clear(_items, 0, _count);
                _count = 0;
            }
        }

        public void CopyTo(T[] array, int arrayIndex) {
            Array.Copy(_items, 0, array, arrayIndex, _count);
        }

        public void CopyTo(T[] array) {
            Array.Copy(_items, 0, array, 0, _count);
        }

        public ReadOnlySpan<T> GetBackingArrayReadOnlySpan() {
            return new ReadOnlySpan<T>(_items, 0, _count);
        }

        public Span<T>.Enumerator GetEnumerator() {
            return new Span<T>(_items, 0, _count).GetEnumerator();
        }

        public T FirstOrDefault(T fallback = default) {
            return _count > 0 ? _items[0] : fallback;
        }
        
        public T[] ToArray() {
            if (_count == 0) {
                return Array.Empty<T>();
            }
            var array = new T[_count];
            Array.Copy(_items, array, _count);
            return array;
        }

#if UNITY_EDITOR
        void BeginDrawListElement(int index) {
            if (index >= _count) {
                GUILayout.Label("Reserved space");
            }
        }

        void EndDrawListElement(int index) {
        }

        void OnItemsArrayValueChanged() {
            if (_items.Length < _count) {
                _count = _items.Length;
            }
        }
#endif
    }
}
