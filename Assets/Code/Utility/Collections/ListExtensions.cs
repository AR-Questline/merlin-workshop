using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Awaken.Utility.Collections {
    public static class ListExtensions {
        public static int FastIndexOf<T, TList>(this TList list, T obj) where TList : IList<T> where T : IEquatable<T> {
            int i = 0;
            int count = list.Count;
            while (i < count && !list[i].Equals(obj)) {
                i++;
            }

            if (i >= count) {
                i = -1;
            }

            return i;
        }
        
        public static int ReverseFastIndexOf<T, TList>(this TList list, T obj) where TList : IList<T> {
            var equalityComparer = EqualityComparer<T>.Default;
            int i = list.Count-1;
            while (i >= 0 && !equalityComparer.Equals(list[i], obj)) {
                i--;
            }

            return i;
        }

        public static bool AddUnique<T>(this List<T> list, T element) {
            if (list.Contains(element)) {
                return false;
            }
            list.Add(element);
            return true;
        }
        
        public static bool AddRangeUnique<T>(this List<T> list, IEnumerable<T> elements) {
            bool isAnyAdded = false;
            foreach (var element in elements) {
                isAnyAdded |= list.AddUnique(element);
            }

            return isAnyAdded;
        }

        public static void AddToEnsureCount<T>(this List<T> list, in T element, in int targetCount) {
            int currentCount = list.Count;
            var elementsToAdd = targetCount - currentCount;
            list.EnsureCapacity(targetCount);
            for (int i = 0; i < elementsToAdd; i++) {
                list.Add(element);
            }
        }
        
        public static void EnsureCapacity<T>(this List<T> list, int capacity) {
            if (list.Capacity >= capacity) return;
            uint doubleCount = list.Count == 0 ? 4 : (uint)list.Count * 2;
            list.Capacity = doubleCount > int.MaxValue ? int.MaxValue : math.max(capacity, (int)doubleCount);
        }

        public static void EnsureCapacityExact<T>(this List<T> list, int capacity) {
            if (list.Capacity >= capacity) return;
            list.Capacity = capacity;
        }
    }
    
    public static class ListExtensions<T> {
        public static List<T> Empty { get; } = new();
    }
}