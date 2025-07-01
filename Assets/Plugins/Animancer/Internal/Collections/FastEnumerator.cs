// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;

namespace Animancer
{
    /// <summary>
    /// An <see cref="IEnumerator{T}"/> for any <see cref="IList{T}"/> doesn't bother checking if the target has been
    /// modified. This gives it good performance but also makes it slightly less safe to use.
    /// </summary>
    /// <remarks>
    /// This struct also implements <see cref="IEnumerable{T}"/> so it can be used in <c>foreach</c> statements and
    /// <see cref="IList{T}"/> to allow the target collection to be modified without breaking the enumerator (though
    /// doing so is still somewhat dangerous so use with care).
    /// </remarks>
    /// <example><code>
    /// var numbers = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, };
    /// var count = 4;
    /// foreach (var number in new FastEnumerator&lt;int&gt;(numbers, count))
    /// {
    ///     Debug.Log(number);
    /// }
    /// 
    /// // Log Output:
    /// // 9
    /// // 8
    /// // 7
    /// // 6
    /// </code></example>
    public struct FastEnumerator<T> : IList<T>, IEnumerator<T>
    {
        /************************************************************************************************************************/

        /// <summary>The target <see cref="IList{T}"/>.</summary>
        private readonly IList<T> List;

        /************************************************************************************************************************/

        private int _Count;

        /// <summary>[<see cref="ICollection{T}"/>]
        /// The number of items in the <see cref="List"/> (which can be less than the
        /// <see cref="ICollection{T}.Count"/> of the <see cref="List"/>).
        /// </summary>
        public int Count
        {
            get => _Count;
            set
            {
                AssertCount(value);
                _Count = value;
            }
        }

        /************************************************************************************************************************/

        private int _Index;

        /// <summary>The position of the <see cref="Current"/> item in the <see cref="List"/>.</summary>
        public int Index
        {
            get => _Index;
            set
            {
                AssertIndex(value);
                _Index = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>The item at the current <see cref="Index"/> in the <see cref="List"/>.</summary>
        public T Current
        {
            get
            {
                AssertCount(_Count);
                AssertIndex(_Index);
                return List[_Index];
            }
            set
            {
                AssertCount(_Count);
                AssertIndex(_Index);
                List[_Index] = value;
            }
        }

        /// <summary>The item at the current <see cref="Index"/> in the <see cref="List"/>.</summary>
        object IEnumerator.Current => Current;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="FastEnumerator{T}"/>.</summary>
        /// <exception cref="NullReferenceException">
        /// The `list` is null. Use the <c>default</c> <see cref="FastEnumerator{T}"/> instead.
        /// </exception>
        public FastEnumerator(IList<T> list)
            : this(list, list.Count)
        {
        }

        /// <summary>Creates a new <see cref="FastEnumerator{T}"/>.</summary>
        /// <exception cref="NullReferenceException">
        /// The `list` is null. Use the <c>default</c> <see cref="FastEnumerator{T}"/> instead.
        /// </exception>
        public FastEnumerator(IList<T> list, int count) : this()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Moves to the next item in the <see cref="List"/> and returns true if there is one.</summary>
        /// <remarks>At the end of the <see cref="List"/> the <see cref="Index"/> is set to <see cref="int.MinValue"/>.</remarks>
        public bool MoveNext()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Moves to the previous item in the <see cref="List"/> and returns true if there is one.</summary>
        /// <remarks>At the end of the <see cref="List"/> the <see cref="Index"/> is set to <c>-1</c>.</remarks>
        public bool MovePrevious()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>[<see cref="IEnumerator"/>] Reverts this enumerator to the start of the <see cref="List"/>.</summary>
        public void Reset()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        void IDisposable.Dispose() {
        }

        /************************************************************************************************************************/
        // IEnumerator.
        /************************************************************************************************************************/

        /// <summary>Returns <c>this</c>.</summary>
        public FastEnumerator<T> GetEnumerator() => this;

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this;

        /************************************************************************************************************************/
        // IList.
        /************************************************************************************************************************/

        /// <summary>[<see cref="IList{T}"/>] Returns the first index of the `item` in the <see cref="List"/>.</summary>
        public int IndexOf(T item) => List.IndexOf(item);

        /// <summary>[<see cref="IList{T}"/>] The item at the specified `index` in the <see cref="List"/>.</summary>
        public T this[int index]
        {
            get
            {
                AssertIndex(index);
                return List[index];
            }
            set
            {
                AssertIndex(index);
                List[index] = value;
            }
        }

        /// <summary>[<see cref="IList{T}"/>] Inserts the `item` at the specified `index` in the <see cref="List"/>.</summary>
        public void Insert(int index, T item)
        {
        }

        /// <summary>[<see cref="IList{T}"/>] Removes the item at the specified `index` from the <see cref="List"/>.</summary>
        public void RemoveAt(int index)
        {
        }

        /************************************************************************************************************************/
        // ICollection.
        /************************************************************************************************************************/

        /// <summary>[<see cref="ICollection{T}"/>] Is the <see cref="List"/> read-only?</summary>
        public bool IsReadOnly => List.IsReadOnly;

        /// <summary>[<see cref="ICollection{T}"/>] Does the <see cref="List"/> contain the `item`?</summary>
        public bool Contains(T item) => List.Contains(item);

        /// <summary>[<see cref="ICollection{T}"/>] Adds the `item` to the end of the <see cref="List"/>.</summary>
        public void Add(T item)
        {
        }

        /// <summary>[<see cref="ICollection{T}"/>] Removes the `item` from the <see cref="List"/> and returns true if successful.</summary>
        public bool Remove(T item)
        {
            return default;
        }

        /// <summary>[<see cref="ICollection{T}"/>] Removes everything from the <see cref="List"/>.</summary>
        public void Clear()
        {
        }

        /// <summary>[<see cref="ICollection{T}"/>] Copies the contents of the <see cref="List"/> into the `array`.</summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Assert-Only] Throws an exception unless 0 &lt;= `index` &lt; <see cref="Count"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        private void AssertIndex(int index)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Assert-Only] Throws an exception unless 0 &lt; `count` &lt;= <see cref="ICollection{T}.Count"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        private void AssertCount(int count)
        {
        }

        /************************************************************************************************************************/
    }
}

