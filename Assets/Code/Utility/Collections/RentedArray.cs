using System;
using System.Buffers;
using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    public readonly struct RentedArray<T> : IDisposable {
        static readonly bool IsValueType = typeof(T).IsValueType;

        readonly T[] _array;
        public readonly int length;

        public bool IsEmpty => length < 1;

        public static RentedArray<T> Borrow(int length) {
            return new(length);
        }

        public static RentedArray<T> Borrow(HashSet<T> source) {
            var array = new RentedArray<T>(source.Count);
            source.CopyTo(array._array);
            return array;
        }

        public static RentedArray<T> Borrow(T[] source) {
            var array = new RentedArray<T>(source.Length);
            Array.Copy(source, array._array, source.Length);
            return array;
        }

        public static RentedArray<T> Borrow(List<T> source) {
            var array = new RentedArray<T>(source.Count);
            source.CopyTo(array._array);
            return array;
        }

        public static RentedArray<T> Borrow(StructList<T> source) {
            var array = new RentedArray<T>(source.Count);
            source.CopyTo(array._array);
            return array;
        }

        RentedArray(int length) {
            if (length < 0) {
                throw new ArgumentException($"Length must be greater or equal zero", nameof(length));
            }
            _array = length == 0 ? Array.Empty<T>() : ArrayPool<T>.Shared.Rent(length);
            this.length = length;
        }

        public Iterator GetEnumerator() {
            return new(_array, length);
        }

        /// <summary>
        /// This array is at lest of size of <see cref="length"/>. <b>So it could be bigger than <see cref="length"/></b>
        /// </summary>
        public T[] GetBackingArray() {
            return _array;
        }

        public void Dispose() {
            if (length > 0) {
                ArrayPool<T>.Shared.Return(_array, !IsValueType);
            }
        }

        public ref T this[int index] => ref _array[index];

        public static implicit operator Span<T>(RentedArray<T> rentedArray) {
            return rentedArray._array.AsSpan(0, rentedArray.length);
        }

        public ref struct Iterator {
            readonly T[] _array;
            readonly int _length;
            int _index;

            public Iterator(T[] array, int length) {
                _array = array;
                _length = length;
                _index = -1;
            }

            public T Current => _array[_index];

            public bool MoveNext() {
                ++_index;
                return _index < _length;
            }
        }
    }
}
