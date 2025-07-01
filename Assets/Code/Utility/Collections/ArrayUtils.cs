using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Object = UnityEngine.Object;

namespace Awaken.Utility.Collections {
    public static class ArrayUtils {
        public static void Insert<T>(ref T[] array, int index, in T item) {
            var newArray = new T[array.Length + 1];
            Array.Copy(array, 0, newArray, 0, index);
            newArray[index] = item;
            Array.Copy(array, index, newArray, index + 1, array.Length - index);
            array = newArray;
        }
        
        public static void Remove<T>(ref T[] array, T item) {
            var index = array.IndexOf(item);
            if (index == -1) {
                return;
            }
            RemoveAt(ref array, index);
        }
        
        public static void RemoveAt<T>(ref T[] array, int index) {
            var newArray = new T[array.Length - 1];
            Array.Copy(array, 0, newArray, 0, index);
            Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);
            array = newArray;
        }
        
        public static void Add<T>(ref T[] array, in T item) {
            var newArray = new T[array.Length + 1];
            Array.Copy(array, 0, newArray, 0, array.Length);
            newArray[array.Length] = item;
            array = newArray;
        }

        public static bool AddUnique<T>(ref T[] array, in T item) {
            if (Array.IndexOf(array, item) == -1) {
                Add(ref array, item);
                return true;
            }
            return false;
        }

        public static void Append<T>(ref T[] array, T[] toAppend, int count) {
            var newArray = new T[array.Length + count];
            Array.Copy(array, 0, newArray, 0, array.Length);
            Array.Copy(toAppend, 0, newArray, array.Length, count);
            array = newArray;
        }
        
        public static void Append<T>(ref T[] array, T[] toAppend) {
            Append(ref array, toAppend, toAppend.Length);
        }

        public static T[] Concat<T>(T[] array0, T[] array1) where T : unmanaged {
            var newArray = new T[array0.Length + array1.Length];
            Array.Copy(array0, 0, newArray, 0, array0.Length);
            Array.Copy(array1, 0, newArray, array0.Length, array1.Length);
            return newArray;
        }
        
        public static T[] Concat<T>(T[] array0, T[] array1, T[] array2) where T : unmanaged {
            var newArray = new T[array0.Length + array1.Length + array2.Length];
            Array.Copy(array0, 0, newArray, 0, array0.Length);
            Array.Copy(array1, 0, newArray, array0.Length, array1.Length);
            Array.Copy(array2, 0, newArray, array0.Length + array1.Length, array2.Length);
            return newArray;
        }
        
        public static T[] Concat<T>(T[] array0, T[] array1, T[] array2, T[] array3) where T : unmanaged {
            var newArray = new T[array0.Length + array1.Length + array2.Length + array3.Length];
            Array.Copy(array0, 0, newArray, 0, array0.Length);
            Array.Copy(array1, 0, newArray, array0.Length, array1.Length);
            Array.Copy(array2, 0, newArray, array0.Length + array1.Length, array2.Length);
            Array.Copy(array3, 0, newArray, array0.Length + array1.Length + array2.Length, array3.Length);
            return newArray;
        }
        
        public static T[] Concat<T>(T[] array0, T[] array1, T[] array2, T[] array3, T[] array4) where T : unmanaged {
            var newArray = new T[array0.Length + array1.Length + array2.Length + array3.Length + array4.Length];
            Array.Copy(array0, 0, newArray, 0, array0.Length);
            Array.Copy(array1, 0, newArray, array0.Length, array1.Length);
            Array.Copy(array2, 0, newArray, array0.Length + array1.Length, array2.Length);
            Array.Copy(array3, 0, newArray, array0.Length + array1.Length + array2.Length, array3.Length);
            Array.Copy(array4, 0, newArray, array0.Length + array1.Length + array2.Length + array3.Length, array4.Length);
            return newArray;
        }
        
        public static T[] Concat<T>(params T[][] arrays) {
            var count = 0;
            for (int i = 0; i < arrays.Length; i++) {
                count += arrays[i].Length;
            }
            var newArray = new T[count];
            count = 0;
            for (int i = 0; i < arrays.Length; i++) {
                Array.Copy(arrays[i], 0, newArray, count, arrays[i].Length);
                count += arrays[i].Length;
            }
            return newArray;
        }

        public static T[] Repeat<T>(T item, int count) {
            var array = new T[count];
            Array.Fill(array, item);
            return array;
        }

        public static T[] Create<T>(int count, Func<int, T> creator) {
            var array = new T[count];
            for (int i = 0; i < count; i++) {
                array[i] = creator(i);
            }
            return array;
        }
        
        public static void EnsureLength<T>(ref T[] array, int length) {
            if (array == null) {
                array = new T[length];
            } else if (array.Length != length) {
                Array.Resize(ref array, length);
            }
        }

        public static T[] CreateCopy<T>(this T[] array) {
            var newArray = new T[array.Length];
            Array.Copy(array, newArray, array.Length);
            return newArray;
        }
        
        public static T2[] Select<T1, T2>(T1[] array, Func<T1, T2> selector) {
            var newArray = new T2[array.Length];
            for (int i = 0; i < array.Length; i++) {
                newArray[i] = selector(array[i]);
            }
            return newArray;
        }
        
        public static T2[] Select<T1, T2>(T1[] array, Func<T1, int, T2> selector) {
            var newArray = new T2[array.Length];
            for (int i = 0; i < array.Length; i++) {
                newArray[i] = selector(array[i], i);
            }
            return newArray;
        }
        
        public static T2[] Select<T1, T2>(List<T1> list, Func<T1, T2> selector) {
            var newArray = new T2[list.Count];
            for (int i = 0; i < list.Count; i++) {
                newArray[i] = selector(list[i]);
            }
            return newArray;
        }
        
        public static T2[] Select<T1, T2>(HashSet<T1> set, Func<T1, T2> selector) {
            var newArray = new T2[set.Count];
            int i = 0;
            foreach (var item in set) {
                newArray[i++] = selector(item);
            }
            return newArray;
        }

        public static bool Equals<T>(T[] lhs, T[] rhs) where T : IEquatable<T> {
            if (lhs.Length != rhs.Length) {
                return false;
            }
            for (int i = 0; i < lhs.Length; i++) {
                if (!lhs[i].Equals(rhs[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool UnityEquals<T>(T[] lhs, T[] rhs) where T : Object {
            if (lhs.Length != rhs.Length) {
                return false;
            }
            for (int i = 0; i < lhs.Length; i++) {
                if (lhs[i] != rhs[i]) {
                    return false;
                }
            }
            return true;
        }
        
        public static int ElementsHashCode<T>(T[] array) {
            if (array == null) {
                return 0;
            }
            var hash = 0;
            for (int i = 0; i < array.Length; i++) {
                hash = (hash * 397) ^ array[i].GetHashCode();
            }
            return hash;
        }

        public static void SquashDuplicatesSorted<T>(ref T[] array) where T : IEquatable<T> {
            T tail = default;
            int index = 0;
            for (int i = 0; i < array.Length; i++) {
                ref var current = ref array[i];
                if (current == null || current.Equals(default) || current.Equals(tail)) {
                    continue;
                }
                array[index++] = tail = current;
            }
            Array.Resize(ref array, index);
        }

        public static bool TryFind<T>(this T[] array, Predicate<T> predicate, out T value) {
            for (int i = 0; i < array.Length; i++) {
                if (predicate(array[i])) {
                    value = array[i];
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static bool TryFind<T>(this T[] array, Predicate<T> predicate, out int index) {
            for (int i = 0; i < array.Length; i++) {
                if (predicate(array[i])) {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
        
        public static T[] CreateArray<T>(List<T> list0, List<T> list1) {
            var arr = new T[list0.Count + list1.Count];
            int insertIndex = 0;
            foreach (var element in list0) {
                arr[insertIndex++] = element;
            }
            foreach (var element in list1) {
                arr[insertIndex++] = element;
            }
            return arr;
        }

        public static ArrayRefIterator<T> RefIterator<T>(this T[] array) => new(array);
        
        public static T[] GetSubArray<T>(this T[] array, int startIndex, int length) {
            if (length == 0) {
                return Array.Empty<T>();
            }
            if (startIndex < 0 || startIndex >= array.Length) {
                CollectionsLogs.LogErrorIndexIsOutOfRangeForCapacity(startIndex, array.Length);
                return Array.Empty<T>();
            }
            var elementsToCopyCount = length - startIndex;
            if (elementsToCopyCount > array.Length) {
                CollectionsLogs.LogErrorTrimmingSubArrayLength(startIndex, length, array.Length);
                length = array.Length - startIndex;
                if (length == 0) {
                    return Array.Empty<T>();
                }
            }
            var subArray = new T[length];
            Array.Copy(array, startIndex, subArray, 0, length);
            return subArray;
        }

        public ref struct ArrayRefIterator<T> {
            T[] _array;
            int _index;
            
            public ArrayRefIterator(T[] array) {
                _array = array;
                _index = -1;
            }
            
            public ArrayRefIterator<T> GetEnumerator() {
                var iterator = this;
                iterator._index = -1;
                return iterator;
            }

            public bool MoveNext() {
                return ++_index < _array.Length;
            }
            
            public ref T Current => ref _array[_index];
        }
    }
}