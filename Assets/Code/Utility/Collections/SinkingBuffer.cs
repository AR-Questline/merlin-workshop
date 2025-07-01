using System.Collections;
using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    public class SinkingBuffer<T> : IEnumerable<T> {
        T[] _array;
        int _head;

        public SinkingBuffer(int count) {
            _array = new T[count];
        }

        public T this[int i] {
            get => _array[(_head + i) % Length];
            set => _array[(_head + i) % Length] = value;
        }

        public void Push(T element) {
            _head = (_head + 1) % Length;
            _array[_head] = element;
        }
        public void Push(T element, out Change change) {
            _head = (_head + 1) % Length;
            change =  new Change(_array[_head], element);
            _array[_head] = element;
        }
        
        public T Bottom => _array[(_head + 1) % Length];
        public T Top => Head;

        public T Head => _array[_head];
        
        public int Length => _array.Length;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>) _array).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public readonly struct Change {
            public readonly T sunk;
            public readonly T added;
            
            public Change(T sunk, T added) {
                this.sunk = sunk;
                this.added = added;
            }
        }
    }
}