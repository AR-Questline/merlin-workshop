using System;
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility.LowLevel.Collections {
    public class UnsafeIndexedSet<T> {
        readonly UnsafePinnableList<T> _bakedList;
        readonly Dictionary<T, Index> _indexByItem;

        public int Count => _bakedList.Count;

        public UnsafeIndexedSet() {
            _bakedList = new UnsafePinnableList<T>();
            _indexByItem = new Dictionary<T, Index>();
        }

        public UnsafeIndexedSet(int initialCapacity) {
            _bakedList = new UnsafePinnableList<T>(initialCapacity);
            _indexByItem = new Dictionary<T, Index>(initialCapacity*2);
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public ref T this[int index] => ref _bakedList[index];

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public bool Add(T item, out Index index) {
            if (_indexByItem.TryGetValue(item, out index)) {
                return false;
            }
            index = new(_bakedList.Count);
            _bakedList.Add(item);
            _indexByItem.Add(item, index);
            return true;
        }

        public void Clear() {
            _bakedList.Clear();
            foreach (var ptr in _indexByItem.Values) {
                ptr.Invalidate();
            }
            _indexByItem.Clear();
        }
        public bool Contains(T item) {
            return _indexByItem.ContainsKey(item);
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public bool Remove(T item) {
            if (_indexByItem.TryGetValue(item, out var indexPtr)) {
                RemoveAt(indexPtr.index);
                return true;
            }
            return false;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public int IntIndexOf(T item) {
            if (_indexByItem.TryGetValue(item, out var indexPtr)) {
                return indexPtr.index;
            }
            return -1;
        }

        public Index IndexOf(T item) {
            if (_indexByItem.TryGetValue(item, out var indexPtr)) {
                return indexPtr;
            }
            return Index.Invalid;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void RemoveAt(int index) {
            // Swap front last item
            var lastItem = _bakedList[^1];
            var lastIndexIndex = _indexByItem[lastItem];
            lastIndexIndex.index = index;
            // Remove item from collections
            var item = _bakedList[index];
            _bakedList.SwapBackRemove(index);
            _indexByItem.Remove(item, out var itemIndex);
            itemIndex.Invalidate();
        }

        public UnsafePinnableList<T>.Enumerator GetEnumerator() {
            return _bakedList.GetEnumerator();
        }

        public class Index : IEquatable<Index> {
            public static readonly Index Invalid = new(-1);

            public int index;
            public bool IsValid => index >= 0;

            public Index(int index) {
                this.index = index;
            }

            public void Invalidate() {
#if DEBUG
                if (index < 0) {
                    return;
                }
                index = ~index;
#else
                index = -1;
#endif
            }

            public static implicit operator int(Index index) => index.index;

            public bool Equals(Index other) {
                return ReferenceEquals(this, other);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) {
                    return false;
                }
                return ReferenceEquals(this, obj);
            }

            public override int GetHashCode() {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return index;
            }

            public static bool operator ==(Index left, Index right) {
                return Equals(left, right);
            }

            public static bool operator !=(Index left, Index right) {
                return !Equals(left, right);
            }
        }
    }
}
