using System;
using Cysharp.Threading.Tasks;

namespace Awaken.Utility.Collections {
    public struct TemporarySet<T> where T : IEquatable<T> {
        readonly T[] _array;
        readonly T _default;

        public event Action changed;
        
        public TemporarySet(int capacity, T defaultValue = default) {
            _array = new T[capacity];
            Array.Fill(_array, defaultValue);
            _default = defaultValue;
            changed = null;
        }

        public void Add(T value, in UniTask duration) {
            AddTask(value, duration).Forget();
        }

        async UniTaskVoid AddTask(T value, UniTask duration) {
            int index = FindFree();
            _array[index] = value;
            changed?.Invoke();
            await duration;
            _array[index] = _default;
            changed?.Invoke();
        }

        int FindFree() {
            int index = Array.IndexOf(_array, _default);
            return index >= 0 ? index : throw new Exception("TemporarySet is full!");
        }

        public readonly Enumerator GetEnumerator() => new(this);

        public struct Enumerator {
            readonly T[] _array;
            readonly T _default;
            int _index;

            public Enumerator(in TemporarySet<T> set) {
                _array = set._array;
                _default = set._default;
                _index = -1;
            }

            public bool MoveNext() {
                while (++_index < _array.Length) {
                    if (!_array[_index].Equals(_default)) {
                        return true;
                    }   
                }
                return false;
            }
            
            public ref T Current => ref _array[_index];
        }
    }

    public static class TemporarySetExtensions {
        public static bool TryGetMin(this in TemporarySet<int> set, out int min) {
            var enumerator = set.GetEnumerator();
            if (!enumerator.MoveNext()) {
                min = default;
                return false;
            }
            min = enumerator.Current;
            while (enumerator.MoveNext()) {
                min = Math.Min(min, enumerator.Current);
            }
            return true;
        }
        
        public static bool TryGetMin(this in TemporarySet<float> set, out float min) {
            var enumerator = set.GetEnumerator();
            if (!enumerator.MoveNext()) {
                min = default;
                return false;
            }
            min = enumerator.Current;
            while (enumerator.MoveNext()) {
                min = Math.Min(min, enumerator.Current);
            }
            return true;
        }
        
        public static bool TryGetMax(this in TemporarySet<int> set, out int max) {
            var enumerator = set.GetEnumerator();
            if (!enumerator.MoveNext()) {
                max = default;
                return false;
            }
            max = enumerator.Current;
            while (enumerator.MoveNext()) {
                max = Math.Max(max, enumerator.Current);
            }
            return true;
        }
        
        public static bool TryGetMax(this in TemporarySet<float> set, out float max) {
            var enumerator = set.GetEnumerator();
            if (!enumerator.MoveNext()) {
                max = default;
                return false;
            }
            max = enumerator.Current;
            while (enumerator.MoveNext()) {
                max = Math.Max(max, enumerator.Current);
            }
            return true;
        }
    }
}