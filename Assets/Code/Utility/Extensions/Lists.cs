using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;

namespace Awaken.Utility.Extensions
{
    public static class Lists {
        public static void Shuffle<T>(this IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
                list.Swap(i, RandomUtil.UniformInt(i, list.Count-1));
        }

        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            (list[i], list[j]) = (list[j], list[i]);
        }

        public static T PopLast<T>(this IList<T> list) {
            int count = list.Count;
            if (count == 0) throw new InvalidOperationException("Can't pop from an empty list.");
            T element = list[count - 1];
            list.RemoveAt(count - 1);
            return element;
        }

        public static int GetIndexOfGreaterOrEqualElementInSortedAsc<T>(this IReadOnlyList<T> list, T target,
            IComparer<T> ascendingComparer) {
            int low = 0, high = list.Count - 1;
            while (low <= high) {
                int mid = low + (high - low) / 2;
                if (ascendingComparer.Compare(list[mid], target) <= 0) {
                    low = mid + 1;
                } else {
                    high = mid - 1;
                }
            }
            
            return low < list.Count ? low : list.Count - 1;
        }

        public static int GetIndexOfSmallerOrEqualElementInSortedDesc<T>(this IReadOnlyList<T> list, T target,
            IComparer<T> descendingComparer) {
            return list.GetIndexOfGreaterOrEqualElementInSortedAsc(target, descendingComparer);
        }
    }
}