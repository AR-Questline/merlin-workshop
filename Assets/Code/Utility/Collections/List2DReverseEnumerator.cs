using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    public struct List2DReverseEnumerator<T> {
        readonly StructList<List<T>> _values;

        int _outerIndex;
        int _innerIndex;

        public List2DReverseEnumerator(StructList<List<T>> values) {
            _values = values;
            _outerIndex = _values.Count-1;
            _innerIndex = _values[_outerIndex].Count; // outer values always have at least one inner list
        }

        public List2DReverseEnumerator<T> GetEnumerator() => this;

        public bool MoveNext() {
            --_innerIndex;

            if (_innerIndex >= 0) {
                return true;
            }

            --_outerIndex;
            if (_outerIndex == -1) {
                return false;
            }

            _innerIndex = _values[_outerIndex].Count;
            return MoveNext();
        }

        public T Current => (T)_values[_outerIndex][_innerIndex];
    }
}