using System;
using System.Collections.Generic;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.LowLevel.Collections {
    [Serializable]
    public class UnsafePinnableList<T> {
        const int DefaultCapacity = 4;

        public static readonly UnsafePinnableList<T> Empty = new();

        [SerializeField, ReadOnly] int _count;
        [SerializeField]
#if UNITY_EDITOR
        [ListDrawerSettings(OnBeginListElementGUI = nameof(BeginDrawListElement), OnEndListElementGUI = nameof(EndDrawListElement))]
        [OnValueChanged(nameof(OnItemsArrayValueChanged))]
#endif
        T[] _items;
        
        static readonly T[] EmptyArray = Array.Empty<T>();
        
        public int Count {
            get => _count;
            set {
                EnsureCapacity(_count);
                _count = value;
            }
        }

        public int Capacity {
            get => _items.Length;
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

        public UnsafePinnableList() {
            _items = EmptyArray;
        }

        public UnsafePinnableList(int capacity) {
            _items = capacity <= 0 ? EmptyArray : new T[capacity];
        }
        
        public static UnsafePinnableList<T> CreateFromRawArray(T[] items) {
            return new UnsafePinnableList<T>() {
                _items = items,
                _count = items.Length
            };
        }
        
        public static TListType CreateFromRawArray<TListType>(T[] items) where TListType : UnsafePinnableList<T>, new() {
            return new TListType() {
                _items = items,
                _count = items.Length
            };
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
        public void AddRepeat(T value, int count) {
            if (_count+count > _items.Length) {
                EnsureCapacity(_count + count);
            }
            Array.Fill(_items, value, _count, count);
            _count += count;
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) {
            return Array.BinarySearch<T>(_items, index, count, item, comparer);
        }

        public int BinarySearch(T item) {
            return BinarySearch(0, Count, item, (IComparer<T>)null);
        }

        public int BinarySearch(T item, IComparer<T> comparer) {
            return BinarySearch(0, Count, item, comparer);
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Clear() {
            if (_count > 0) {
                Array.Clear((Array)_items, 0, _count);
                _count = 0;
            }
        }

        public bool Contains(T item) {
            EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            return Contains(item, equalityComparer);
        }

        public bool Contains(T item, EqualityComparer<T> equalityComparer) {
            for (int index = 0; index < _count; ++index) {
                if (equalityComparer.Equals(_items[index], item)) {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array) {
            CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            Array.Copy(_items, 0, array, arrayIndex, _count);
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

        public bool Exists(Predicate<T> match) {
            return FindIndex(match) != -1;
        }

        public T Find(Predicate<T> match) {
            for (int index = 0; index < _count; ++index) {
                if (match(_items[index])) {
                    return _items[index];
                }
            }
            return default;
        }

        public int FindIndex(Predicate<T> match) {
            return FindIndex(0, _count, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match) {
            return FindIndex(startIndex, _count - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match) {
            int num = startIndex + count;
            for (int index = startIndex; index < num; ++index) {
                if (match(_items[index])) {
                    return index;
                }
            }
            return -1;
        }

        public T FindLast(Predicate<T> match) {
            for (int index = _count - 1; index >= 0; --index) {
                if (match(_items[index])) {
                    return _items[index];
                }
            }
            return default;
        }

        public T FirstOrDefault() {
            if (_count > 0){
                return _items[0];
            }

            return default;
        }

        public Enumerator GetEnumerator() {
            return new(this);
        }

        public int IndexOf(T item) {
            return Array.IndexOf(_items, item, 0, _count);
        }

        public int IndexOf(T item, int index) {
            return Array.IndexOf(_items, item, index, _count - index);
        }

        public int IndexOf(T item, EqualityComparer<T> comparer) {
            for (int i = 0; i < _count; i++) {
                if (comparer.Equals(item, _items[i])) {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOf(T item, int index, int count) {
            return Array.IndexOf(_items, item, index, count);
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

        public int LastIndexOf(T item) {
            return _count == 0 ? -1 : LastIndexOf(item, _count - 1, _count);
        }

        public int LastIndexOf(T item, int index) {
            return LastIndexOf(item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count) {
            return Array.LastIndexOf<T>(_items, item, index, count);
        }

        public bool Remove(T item) {
            int index = IndexOf(item);
            if (index < 0) {
                return false;
            }
            RemoveAt(index);
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

        public void SwapBackRemove(int index) {
            var lastIndex = _count - 1;
            _items[index] = _items[lastIndex];
            _items[lastIndex] = default;
            --_count;
        }

        public void Reverse() {
            Reverse(0, Count);
        }

        public void Reverse(int index, int count) {
            Array.Reverse((Array)_items, index, count);
        }

        public void Sort() {
            Sort(0, Count, (IComparer<T>)null);
        }

        public void Sort(IComparer<T> comparer) {
            Sort(0, Count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer) {
            Array.Sort<T>(_items, index, count, comparer);
        }

        public Span<T> AsSpan() {
            return new Span<T>(_items, 0, _count);
        }

        public T[] ToArray() {
            T[] destinationArray = new T[_count];
            Array.Copy((Array)_items, 0, (Array)destinationArray, 0, _count);
            return destinationArray;
        }

        public void TrimExcess() {
            if (_count >= (int)((double)_items.Length * 0.9)) {
                return;
            }
            Capacity = _count;
        }
        
        public T[] GetInternalArray() {
            return _items;
        }
        
#if UNITY_EDITOR
        void BeginDrawListElement(int index) {
            if (index > _count) {
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
        
        [Serializable, Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public ref struct Enumerator {
            UnsafePinnableList<T> _list;
            int _index;

            internal Enumerator(UnsafePinnableList<T> list) {
                _list = list;
                _index = -1;
            }

            public void Dispose() {}

            public bool MoveNext() {
                return ++_index < _list._count;
            }

            public ref T Current => ref _list[_index];
        }
    }
}
