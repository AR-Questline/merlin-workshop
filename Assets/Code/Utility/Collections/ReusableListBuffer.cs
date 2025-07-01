using System;
using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    public readonly struct ReusableListBuffer<T> : IDisposable {
        readonly List<T> _buffer;
        public List<T> Buffer => _buffer;
        public int Count => _buffer.Count;

        public ReusableListBuffer(List<T> buffer) {
            _buffer = buffer;
            _buffer.Clear();
        }

        public T this[int i] {
            get => _buffer[i];
            set => _buffer[i] = value;
        }

        public void Dispose() {
            _buffer.Clear();
        }

        public List<T>.Enumerator GetEnumerator() {
            return _buffer.GetEnumerator();
        }

        public static implicit operator List<T>(ReusableListBuffer<T> reusableListBuffer) {
            return reusableListBuffer._buffer;
        }
    }
}
