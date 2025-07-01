using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility.Collections {
    [Il2CppEagerStaticClassConstruction]
    public class HierarchicalDictionary<TKey, TValue> : Dictionary<TKey, StructList<List<TValue>>> {
        public static readonly StructList<List<TValue>> EmptyValues = new(new[] { ListExtensions<TValue>.Empty });

        readonly int _outerCapacity;
        readonly int _innerCapacity;

        public ref readonly StructList<List<TValue>> EmptyDefault => ref EmptyValues;

        public HierarchicalDictionary(int capacity, int outerCapacity, int innerCapacity) : base(capacity) {
            _outerCapacity = outerCapacity;
            _innerCapacity = innerCapacity;
        }

        public void Add(TKey[] hierarchy, TValue value) {
            List<TValue> ownValues;
            var addedOwnValues = false;
            if (!TryGetValue(hierarchy[0], out var values)) {
                values = new StructList<List<TValue>>(_outerCapacity);

                ownValues = new List<TValue>(_innerCapacity);
                addedOwnValues = true;
                values.Add(ownValues);

                TryAdd(hierarchy[0], values);
            } else {
                ownValues = values[0];
                if (ownValues == ListExtensions<TValue>.Empty) {
                    ownValues = new List<TValue>(_innerCapacity);
                    addedOwnValues = true;
                    values[0] = ownValues;
                }
            }

            ownValues.Add(value);

            if (addedOwnValues) {
                InitializeHierarchy(hierarchy, ownValues);
            }
        }

        public void Remove(TKey key, TValue value) {
            if (TryGetValue(key, out var values)) {
                values[0].Remove(value);
            }
        }

        public StructList<List<TValue>> GetOrDefault(TKey key, StructList<List<TValue>> defaultValue = default) {
            return TryGetValue(key, out var values) ? values : defaultValue;
        }

        public List2DEnumerator<TValue> Enumerate(TKey key) {
            if (TryGetValue(key, out StructList<List<TValue>> values)) {
                return new List2DEnumerator<TValue>(values);
            } else {
                return new List2DEnumerator<TValue>(EmptyValues);
            }
        }

        public void InitCapacity(TKey[] hierarchy, ushort hierarchyCapacity, ushort ownCapacity) {
            if (!TryGetValue(hierarchy[0], out var values)) {
                values = new StructList<List<TValue>>(hierarchyCapacity);

                var ownValues = ownCapacity == 0 ? ListExtensions<TValue>.Empty : new List<TValue>(ownCapacity);
                values.Add(ownValues);

                TryAdd(hierarchy[0], values);

                if (ownCapacity > 0) {
                    InitializeHierarchy(hierarchy, ownValues);
                }
            } else {
                values.EnsureCapacityExact(hierarchyCapacity);
                values[0].EnsureCapacityExact(ownCapacity);
            }
        }

        void InitializeHierarchy(TKey[] hierarchy, List<TValue> newValues) {
            for (int i = 1; i < hierarchy.Length; i++) {
                var parentKey = hierarchy[i];
                if (!TryGetValue(parentKey, out var parentValues)) {
                    parentValues = new StructList<List<TValue>>(_outerCapacity);

                    // Own values are always first
                    parentValues.Add(ListExtensions<TValue>.Empty);

                    TryAdd(parentKey, parentValues);
                }

                parentValues.Add(newValues);
                this[parentKey] = parentValues;
            }
        }
    }
}
