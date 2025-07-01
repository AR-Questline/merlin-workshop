using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    // === This code copied from http://stackoverflow.com/questions/380595/multimap-in-net

    /// <summary>
    /// Extension to the normal Dictionary. This class can store more than one value for every key. It keeps a HashSet for every Key value.
    /// Calling Add with the same Key and multiple values will store each value under the same Key in the Dictionary. Obtaining the values
    /// for a Key will return the HashSet with the Values of the Key. 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class MultiMap<TKey, TValue> : Dictionary<TKey, HashSet<TValue>> {
        static readonly HashSet<TValue> EmptyValues = new();
        int _valuesHashSetInitialCapacity;
        IEqualityComparer<TValue> _valuesComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        public MultiMap() {
            _valuesComparer = EqualityComparer<TValue>.Default;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        public MultiMap(int capacity, int valuesInitialCapacity = 16) : base(capacity) {
            _valuesComparer = EqualityComparer<TValue>.Default;
            _valuesHashSetInitialCapacity = valuesInitialCapacity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMap&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        public MultiMap(IEqualityComparer<TKey> comparer) : base(comparer) {
            _valuesComparer = EqualityComparer<TValue>.Default;
        }

        public MultiMap(IEqualityComparer<TValue> valuesComparer) {
            _valuesComparer = valuesComparer;
        }

        /// <summary>
        /// Adds the specified value under the specified key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value) {
            if (!this.TryGetValue(key, out HashSet<TValue> container)) {
                container = new HashSet<TValue>(_valuesHashSetInitialCapacity, _valuesComparer);
                base.Add(key, container);
            }

            container.Add(value);
        }

        public void AddAll(TKey key, IEnumerable<TValue> values) {
            if (!this.TryGetValue(key, out HashSet<TValue> container)) {
                container = new HashSet<TValue>(_valuesHashSetInitialCapacity, _valuesComparer);
                base.Add(key, container);
            }

            foreach (TValue value in values) {
                container.Add(value);
            }
        }

        /// <summary>
        /// Determines whether this dictionary contains the specified value for the specified key 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the value is stored for the specified key in this dictionary, false otherwise</returns>
        public bool ContainsValue(TKey key, TValue value) {
            bool toReturn = false;
            if (this.TryGetValue(key, out HashSet<TValue> values)) {
                toReturn = values.Contains(value);
            }

            return toReturn;
        }

        /// <summary>
        /// Removes the specified value for the specified key. It will leave the key in the dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Remove(TKey key, TValue value) {
            if (this.TryGetValue(key, out HashSet<TValue> container)) {
                container.Remove(value);
                if (container.Count <= 0) {
                    this.Remove(key);
                }
            }
        }

        public void RemoveAll(TKey key) {
            Remove(key);
        }

        /// <summary>
        /// Merges the specified MultiMap into this instance.
        /// </summary>
        /// <param name="toMergeWith">To merge with.</param>
        public void Merge(MultiMap<TKey, TValue> toMergeWith) {
            if (toMergeWith == null) {
                return;
            }

            foreach (KeyValuePair<TKey, HashSet<TValue>> pair in toMergeWith) {
                foreach (TValue value in pair.Value) {
                    this.Add(pair.Key, value);
                }
            }
        }

        /// <summary>
        /// Gets the values for the key specified. This method is useful if you want to avoid an exception for key value retrieval and you can't use TryGetValue
        /// (e.g. in lambdas)
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="returnEmptySet">if set to true and the key isn't found, an empty hashset is returned, otherwise, if the key isn't found, null is returned</param>
        /// <returns>
        /// This method will return null (or an empty set if returnEmptySet is true) if the key wasn't found, or
        /// the values if key was found.
        /// </returns>
        public HashSet<TValue> GetValues(TKey key, bool returnEmptySet) {
            if (!TryGetValue(key, out HashSet<TValue> toReturn) && returnEmptySet) {
                return EmptyValues;
            }
            return toReturn;
        }

        public IEnumerable<TValue> InnerValues {
            get {
                foreach (var set in Values) {
                    foreach (TValue value in set) {
                        yield return value;
                    }
                }
            }
        }
    }
}