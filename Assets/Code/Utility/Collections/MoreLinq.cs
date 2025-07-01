using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.Utility.Collections
{
    public static class MoreLinq
    {
        public static int IndexOf<T>(this IEnumerable<T> elements, T element) {
            int index = 0;
            // search linearly
            if (element == null) {
                foreach (T e in elements) {
                    if (e == null) return index;
                    index++;
                }
            } else {
                foreach (T e in elements) {
                    if (element.Equals(e)) return index;
                    index++;
                }
            }
            // nothing found
            return -1;
        }
        
        public static int IndexOf<T>(this IEnumerable<T> elements, Func<T, bool> predicate) {
            int index = 0;
            // search linearly
            foreach (T e in elements) {
                if (predicate(e)) return index;
                index++;
            }
            // nothing found
            return -1;
        }

        public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, out T result) {
            using var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext()) {
                result = enumerator.Current;
                return true;
            }
            result = default;
            return false;
        }
        
        public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate, out T result) {
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) {
                result = enumerator.Current;
                if (predicate(result)) {
                    return true;
                }
            }
            result = default;
            return false;
        }

        public static T MinBy<T, TKey>(this IEnumerable<T> elements, Func<T, TKey> key, bool safeMode = false) where TKey : IComparable<TKey> {
            using var enumerator = elements.GetEnumerator();
            // use the first element to initialize
            if (!enumerator.MoveNext()) {
                if (safeMode) {
                    return default;
                } else {
                    throw new ArgumentException("Cannot find the minimum of an empty collection.");
                }
            }
            T minimumElement = enumerator.Current;
            TKey minimumKey = key(minimumElement);
            // iterate over the other elements and update
            while (enumerator.MoveNext()) {
                T element = enumerator.Current;
                TKey elementKey = key(element);
                if (elementKey.CompareTo(minimumKey) < 0) {
                    minimumElement = element;
                    minimumKey = elementKey;
                }
            }
            // done!
            return minimumElement;
        }

        public static T MaxBy<T, TKey>(this IEnumerable<T> elements, Func<T, TKey> key, bool safeMode = false) where TKey : IComparable<TKey> {
            using var enumerator = elements.GetEnumerator();
            // use the first element to initialize
            if (!enumerator.MoveNext()) {
                if (safeMode) {
                    return default;
                } else {
                    throw new ArgumentException("Cannot find the maximum of an empty collection.");
                }
            }
            T maximumElement = enumerator.Current;
            TKey maximumKey = key(maximumElement);
            // iterate over the other elements and update
            while (enumerator.MoveNext()) {
                T element = enumerator.Current;
                TKey elementKey = key(element);
                if (elementKey.CompareTo(maximumKey) > 0) {
                    maximumElement = element;
                    maximumKey = elementKey;
                }
            }
            // done!
            return maximumElement;
        }

        [LinqTunnel]
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source) {
                if (seenKeys.Add(keySelector(element))) {
                    yield return element;
                }
            }
        }

        public static IOrderedEnumerable<TSource> OrderWith<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer) {
            return source.OrderBy(element => element, comparer);
        }

        public static void ForEach<T>(this IEnumerable<T> elements, Action<T> action) {
            foreach (T element in elements) {
                action(element);
            }
        }
        [LinqTunnel]
        public static IEnumerable<T> ForEachDeferred<T>(this IEnumerable<T> elements, Action<T> action) {
            foreach (T element in elements) {
                action(element);
                yield return element;
            }
        }
        
        public static void ForEach<T>(this List<T> elements, Action<T> action) {
            foreach (T element in elements) {
                action(element);
            }
        }
        
        public static void ForEach<T>(this T[] elements, Action<T> action) {
            foreach (T element in elements) {
                action(element);
            }
        }
        
        public static void ForEach<T>(this HashSet<T> elements, Action<T> action) {
            foreach (T element in elements) {
                action(element);
            }
        }

        public static void ForEachRef<T>(this T[] elements, ActionRef<T> action) where T : struct {
            for (int i = 0; i < elements.Length; i++) {
                ref T element = ref elements[i];
                action(ref element);
            }
        }

        public static bool IsEmpty<T>(this ICollection<T> list) => list.Count == 0;
        
        public static bool IsNotEmpty<T>(this ICollection<T> list) => list.Count > 0;

        public static bool IsNullOrEmpty<T>(this ICollection<T> list) => list == null || list.Count == 0;
        
        public static bool IsNullOrUnityEmpty<T>(this ICollection<T> list) where T : Object => list == null || list.Count == 0 || list.All(i => i == null);

        public static bool IsNotNullOrEmpty<T>(this ICollection<T> list) => list is { Count: > 0 };

        public static bool AtLeast<T>(this IEnumerable<T> source, int minCount) {
            var collection = source as ICollection<T>;
            return collection == null
                ? source.Skip(minCount - 1).Any()
                : collection.Count >= minCount;
        }

        [LinqTunnel]
        public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n) {
            var it = source.GetEnumerator();
            bool hasRemainingItems;
            var cache = new Queue<T>(n + 1);

            do {
                hasRemainingItems = it.MoveNext();
                if (hasRemainingItems) {
                    cache.Enqueue(it.Current);
                    if (cache.Count > n) {
                        yield return cache.Dequeue();
                    }
                }
            } while (hasRemainingItems);

            it.Dispose();
        }

        public static T FirstOrAny<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : class {
            T itemThatSatisfyPredicate = source.FirstOrDefault(predicate);
            if (itemThatSatisfyPredicate == null) {
                return source.FirstOrDefault();
            } else {
                return itemThatSatisfyPredicate;
            }
        }

        public static T FirstOrFallback<T>(this IEnumerable<T> source, T fallback) where T : class {
            T itemThatSatisfyPredicate = source.FirstOrDefault();
            if (itemThatSatisfyPredicate == null) {
                return fallback;
            } else {
                return itemThatSatisfyPredicate;
            }
        }

        public static T FirstOrFallback<T>(this IEnumerable<T> source, Func<T, bool> predicate, T fallback) where T : class {
            T itemThatSatisfyPredicate = source.FirstOrDefault(predicate);
            if (itemThatSatisfyPredicate == null) {
                return fallback;
            } else {
                return itemThatSatisfyPredicate;
            }
        }

        public static Transform FirstOrDefault(this Transform transform, Func<Transform, bool> query) {
            if (query(transform)) {
                return transform;
            }
            for (int i = 0; i < transform.childCount; i++) {
                var result = FirstOrDefault(transform.GetChild(i), query);
                if (result != null) {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Ignores Unity nulls
        /// </summary>
        [LinqTunnel]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) => source.Where(i => i != null);
        
        /// <summary>
        /// Supports Unity's null checking
        /// </summary>
        [LinqTunnel]
        public static IEnumerable<T> WhereNotUnityNull<T>(this IEnumerable<T> source) => source.Where(i => i is UnityEngine.Object obj ? obj != null : i != null);

        [LinqTunnel]
        public static IEnumerable<TResult> SelectWithLog<TInput, TResult>(this IEnumerable<TInput> source, Func<TInput, TResult> predicate, string prefix = "[Select]", string suffix = " Select failed on:", Debugging.LogType logType = Debugging.LogType.Debug) where TInput : Component {
            foreach (TInput element in source) {
                TResult result = predicate(element);
                if (result == null) {
#if UNITY_EDITOR
#pragma warning disable CS0618 // Type or member is obsolete
                    Debugging.Log.When(logType)?.Warning(prefix + " '" + element?.gameObject.PathInSceneHierarchy() + "' " + suffix, element, LogOption.NoStacktrace);
#pragma warning restore CS0618 // Type or member is obsolete
#endif
                } else {
                    yield return result;
                }
            }
        }

        public static T FirstNotNull<T>(params T[] args) where T : class => args.FirstOrDefault(a => a != null);
        public static T FirstNotNull<T>(Func<T, bool> predicate, params T[] args) where T : class => args.FirstOrDefault(a => a != null && predicate(a));

        public static T PreviousItem<T>(this IEnumerable<T> source, T item, bool carousel = false) where T : class {
            var prevItem = source.TakeWhile(t => t != item).LastOrDefault();
            if (prevItem == null && carousel) {
                return source.LastOrDefault();
            }
            return prevItem;
        }
        
        public static T NextItem<T>(this IEnumerable<T> source, T item, bool carousel = false) where T : class {
            var nextItem = source.SkipWhile(t => t != item).Skip(1).FirstOrDefault();
            if (nextItem == null && carousel) {
                return source.FirstOrDefault();
            }

            return nextItem;
        }
        
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> range) {
            foreach (var element in range) {
                hashSet.Add(element);
            }
        }

        public static T Only<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {
            predicate ??= _ => true;
            T output = default;
            bool set = false;
            foreach (var t in enumerable.Where(predicate)) {
                if (set) {
                    throw new MultipleMatchException("More than one element matches given query");
                }
                output = t;
                set = true;
            }

            if (set) {
                return output;
            } else {
                throw new NoMatchException("No element matches given query");
            }
        }

        public static bool TryGetOnly<TOriginal, TResult>(this IEnumerable<TOriginal> enumerable, out TResult result) where TOriginal : class where TResult : TOriginal {
            result = default;
            bool set = false;
            foreach (var t in enumerable) {
                if (t is not TResult tResult) {
                    continue;
                }
                if (set) {
                    return false;
                }
                result = tResult;
                set = true;
            }

            return set;
        }

        public static TResult TryGetOnly<TOriginal, TResult>(this IEnumerable<TOriginal> enumerable) where TOriginal : class where TResult : class, TOriginal {
            return enumerable.TryGetOnly(out TResult result) ? result : null;
        }

        public static bool TryGetOnly<TElement>(this IEnumerable<TElement> enumerable, Func<TElement, bool> predicate, out TElement result) {
            result = default;
            bool set = false;
            foreach (var element in enumerable) {
                if (!predicate(element)) {
                    continue;
                }
                if (set) {
                    return false;
                }
                result = element;
                set = true;
            }

            return set;
        }
        
        public static TElement TryGetOnly<TElement>(this IEnumerable<TElement> enumerable, Func<TElement, bool> predicate) where TElement : class {
            return enumerable.TryGetOnly(predicate, out var result) ? result : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIn<T>(this T item, IEnumerable<T> collection) {
            return collection.Contains(item);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIn<T>(this T item, List<T> collection) {
            return collection.Contains(item);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIn<T>(this T item, T[] collection) {
            return Array.IndexOf(collection, item) > -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotIn<T>(this T item, IEnumerable<T> collection) {
            return !item.IsIn(collection);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotIn<T>(this T item, List<T> collection) {
            return !item.IsIn(collection);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotIn<T>(this T item, T[] collection) {
            return !item.IsIn(collection);
        }

        public static int GetSequenceHashCode<T>(this IEnumerable<T> sequence) {
            return sequence.Aggregate(0, (hash, element) => hash * 31 + element?.GetHashCode() ?? 0);
        }

        public static bool SequenceEquals<T>(List<T> lhs, List<T> rhs) where T : IEquatable<T> {
            if (lhs.Count != rhs.Count) {
                return false;
            }
            for (int i = 0; i < lhs.Count; i++) {
                if (!lhs[i].Equals(rhs[i])) {
                    return false;
                }
            }
            return true;
        }

        public static float AverageSafe<T>(this IEnumerable<T> source, Func<T, float> selector) {
            float sum = 0;
            int count = 0;
            foreach (var element in source) {
                sum += selector(element);
                count++;
            }
            return count == 0 ? float.NaN : sum / count;
        }

        // === BitArray

        public static int CountOnes(this BitArray array) {
            int count = 0;
            for (int i = 0; i < array.Length; i++) {
                if (array[i]) {
                    count++;
                }
            }
            return count;
        }
        
        public static int CountZeros(this BitArray array) {
            int count = 0;
            for (int i = 0; i < array.Length; i++) {
                if (!array[i]) {
                    count++;
                }
            }
            return count;
        }

        public static bool Any(this BitArray array) {
            for(int i = 0; i < array.Length; i++) {
                if (array[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    public class MultipleMatchException : ArgumentException {
        public MultipleMatchException(string message) : base(message) { }
    }
    public class NoMatchException : ArgumentException {
        public NoMatchException(string message) : base(message) { }
    }

    public delegate void ActionRef<T>(ref T item) where T : struct;
}