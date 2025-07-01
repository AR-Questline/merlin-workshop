using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    // === This code copied from http://stackoverflow.com/questions/380595/multimap-in-net

    /// <summary>
    /// Extension to the normal Dictionary. This class can store more than one value for every key. It keeps a List for every Key value,
    /// which allows adding by index.
    /// Calling Add with the same Key and multiple values will store each value under the same Key in the Dictionary. Obtaining the values
    /// for a Key will return the list of all Values of the Key, in order of addition by default (unless AddAt() was used).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class IndexedMultiMap<TKey, TValue> : Dictionary<TKey, List<TValue>> {
        static readonly List<TValue> EmptyValues = new();
        
        /// <summary>
        /// Adds the specified value under the specified key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValue value) {
            if (!this.TryGetValue(key, out List<TValue> container)) {
                container = new List<TValue>();
                base.Add(key, container);
            }
            container.Add(value);
        }

        public void AddAt(TKey key, int index, TValue value) {
            if (!this.TryGetValue(key, out List<TValue> container)) {
                container = new List<TValue>();
                base.Add(key, container);
            }
            container.Insert(index, value);
        }

        public void AddAll(TKey key, IEnumerable<TValue> values) {
            if (!this.TryGetValue(key, out List<TValue> container)) {
                container = new List<TValue>();
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
            if (this.TryGetValue(key, out List<TValue> values)) {
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
            if (this.TryGetValue(key, out List<TValue> container)) {
                container.Remove(value);
                if (container.Count <= 0) {
                    this.Remove(key);
                }
            }
        }

        /// <summary>
        /// Merges the specified MultiMap into this instance.
        /// </summary>
        /// <param name="toMergeWith">To merge with.</param>
        public void Merge(IDictionary<TKey, IEnumerable<TValue>> toMergeWith) {
            if (toMergeWith == null) {
                return;
            }

            foreach (KeyValuePair<TKey, IEnumerable<TValue>> pair in toMergeWith) {
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
        /// <param name="returnEmpty">if set to true and the key isn't found, an empty hashset is returned, otherwise, if the key isn't found, null is returned</param>
        /// <returns>
        /// This method will return null (or an empty set if returnEmpty is true) if the key wasn't found, or
        /// the values if key was found.
        /// </returns>
        public List<TValue> GetValues(TKey key, bool returnEmpty) {
            if (!TryGetValue(key, out var toReturn) && returnEmpty) {
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
