using System;
using System.Collections.Generic;

namespace UnityEditor.Localization.Plugins.XLIFF.Common
{
    static class ListExtensions
    {
        /// <summary>
        /// Returns the number of items of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int GetItemCount<T>(this IList<object> list)
        {
            if (list == null)
                return 0;

            int count = 0;
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is T)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Looks for the n item of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T GetItem<T>(this IList<object> list, int n)
        {
            int itemIndex = 0;
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is T item)
                {
                    if (itemIndex == n)
                        return item;
                    itemIndex++;
                }
            }

            throw new IndexOutOfRangeException($"Could not find item {n} of type {typeof(T).FullName} in list of size {list.Count}.");
        }
    }
}
