/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using System.Collections.Generic;

namespace Awaken.TG.Utility.Maths {
    /// <summary>
    /// The QuickSelect algorithm is a selection algorithm that uses a similar method as the QuickSort sorting algorithm. More information can be found on this page:
    /// https://en.wikipedia.org/wiki/Quickselect. 
    /// </summary>
    public class QuickSelect {
        /// <summary>
        /// Returns the element that is the Kth smallest within the array.
        /// </summary>
        /// <param name="array">The array that should be searched.</param>
        /// <param name="arrayCount">The number of elements to search within the array.</param>
        /// <param name="k">The nth smallest value to retrieve. 0 indicates the smallest element, endIndex - 1 indicates the largest.</param>
        /// <param name="comparer">The IComparer used to compare the array.</param>
        /// <returns>The element that is the Kth smallest within the array.</returns>
        public static T SmallestK<T>(T[] array, int arrayCount, int k, IComparer<T> comparer) {
            if (k > arrayCount - 1) {
                k = arrayCount - 1;
            }

            return SmallestK<T>(array, 0, arrayCount - 1, k, comparer);
        }

        /// <summary>
        /// Returns the element that is the Kth smallest within the array.
        /// </summary>
        /// <param name="array">The array that should be searched.</param>
        /// <param name="startIndex">The starting index of the array.</param>
        /// <param name="endIndex">The ending index of the array.</param>
        /// <param name="k">The nth smallest value to retrieve. 0 indicates the smallest element, endIndex - 1 indicates the largest.</param>
        /// <param name="comparer">The IComparer used to compare the array.</param>
        /// <returns>The element that is the Kth smallest within the array.</returns>
        private static T SmallestK<T>(T[] array, int startIndex, int endIndex, int k, IComparer<T> comparer) {
            if (startIndex == endIndex) {
                return array[startIndex];
            }

            // Similar to the QuickSort algorithm, split the array into a subset and reorder based on the pivot.
            var pivotIndex = Partition(array, startIndex, endIndex, comparer);

            // If the pivot is same as k then the kth smallest value has been found. 
            if (pivotIndex == k) {
                return array[pivotIndex];
            }

            // If the pivot is less, then the Kth smallest element is in the right subgroup.
            if (pivotIndex < k) {
                return SmallestK(array, pivotIndex + 1, endIndex, k, comparer);
            }

            // If the pivot is greater, then the Kth smallest element is in the left subgroup.
            return SmallestK<T>(array, startIndex, pivotIndex - 1, k, comparer);
        }

        /// <summary>
        /// Partition the array into two groups based on the pivot. All values smaller than the pivot will be moved to the left, and all values greater will be moved
        /// to the right. This is similar to the QuickSort algorithm.
        /// </summary>
        /// <param name="array">The array that should be sorted.</param>
        /// <param name="startIndex">The starting index of the array.</param>
        /// <param name="endIndex">The ending index of the array.</param>
        /// <param name="k">The nth smallest value to retrieve. 0 indicates the smallest element, endIndex - 1 indicates the largest.</param>
        /// <param name="comparer">The IComparer used to compare the array.</param>
        /// <returns>The position of the pivot.</returns>
        private static int Partition<T>(T[] array, int startIndex, int endIndex, IComparer<T> comparer) {
            var pivotIndex = UnityEngine.Random.Range(startIndex, endIndex + 1);
            // The pivot has not been reordered yet. Move all elements that are less than the pivot to the left, and move all elements that are greater to the right.
            var pivotValue = array[pivotIndex];
            // The pivot should be moved to the end so it won't be compared against itself.
            Swap(array, pivotIndex, endIndex);
            var index = startIndex;
            for (int i = startIndex; i < endIndex; ++i) {
                if (comparer.Compare(array[i], pivotValue) < 0) {
                    Swap(array, index, i);
                    index++;
                }
            }

            // Ensure the pivot is on the right of the smaller values.
            Swap(array, index, endIndex);
            return index;
        }

        /// <summary>
        /// Swap the first and second elements.
        /// </summary>
        /// <param name="array">The array that should be sorted.</param>
        /// <param name="firstIndex">The first index that should be swapped.</param>
        /// <param name="secondIndex">The second index that should be swapped.</param>
        static void Swap<T>(IList<T> array, int firstIndex, int secondIndex) {
            (array[firstIndex], array[secondIndex]) = (array[secondIndex], array[firstIndex]);
        }
    }
}