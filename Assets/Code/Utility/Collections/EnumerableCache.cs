#if UNITY_EDITOR || DEBUG
#define CHECK_IN_USE
#endif

using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Awaken.Utility.Collections {
    public class EnumerableCache<T> {
        T[] _backingArray;
        int _length;
        
#if CHECK_IN_USE
        bool _inUse;
#endif

        public EnumerableCache(int capacity) {
            _backingArray = new T[math.ceilpow2(capacity)];
#if CHECK_IN_USE
            _inUse = false;
#endif
        }
        
        public Enumerator this[IEnumerable<T> enumerable] => Cache(enumerable);

        public Enumerator Cache<TEnumerable>(TEnumerable enumerable) where TEnumerable : IEnumerable<T> {
#if CHECK_IN_USE
            if (_inUse) {
                throw new Exception("Invalid use of EnumerableCache");
            }
            _inUse = true;
#endif
            _length = 0;
            int capacity = _backingArray.Length;
            foreach (var t in enumerable) {
                if (_length == capacity) {
                    capacity = capacity + capacity;
                    Array.Resize(ref _backingArray, capacity);
                }
                _backingArray[_length++] = t;
            }
            return new Enumerator(this);
        }

        public ref struct Enumerator {
            EnumerableCache<T> _cache;
            int _index;

            public Enumerator(EnumerableCache<T> cache) {
                _cache = cache;
                _index = -1;
            }

            public Enumerator GetEnumerator() => this;

            public bool MoveNext() => ++_index < _cache._length;

            public T Current => _cache._backingArray[_index];

            public void Reset() => _index = -1;
        
            [UnityEngine.Scripting.Preserve] 
            public void Dispose() {
                Array.Clear(_cache._backingArray, 0, _cache._length);
                _cache._length = 0;
#if CHECK_IN_USE
                _cache._inUse = false;
#endif
            }
        }
    }
}