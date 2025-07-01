using System.Collections.Generic;

namespace Awaken.Utility.Collections {
    public struct List2DEnumerator<T> {
        readonly StructList<List<T>> _values;

        int _outerCount;
        int _innerCount;

        int _outerIndex;
        int _innerIndex;

        public List2DEnumerator(StructList<List<T>> values) {
            _values = values;
            _outerCount = _values.Count;
            _innerCount = _values[0].Count;
            _outerIndex = 0;
            _innerIndex = -1;
        }

        List2DEnumerator(StructList<List<T>> values, int outerCount, int innerCount, int outerIndex, int innerIndex) {
            _values = values;
            _outerCount = outerCount;
            _innerCount = innerCount;
            _outerIndex = outerIndex;
            _innerIndex = innerIndex;
        }

        public List2DEnumerator<T> GetEnumerator() => this;

        public bool MoveNext() {
            ++_innerIndex;

            if (_innerIndex < _innerCount) {
                return true;
            }

            ++_outerIndex;
            if (_outerIndex >= _outerCount) {
                return false;
            }

            _innerCount = _values[_outerIndex].Count;
            _innerIndex = -1;
            return MoveNext();
        }

        public T Current => _values[_outerIndex][_innerIndex];

        public List2DEnumerator<T> Copy() {
            return new List2DEnumerator<T>(_values, _outerCount, _innerCount, _outerIndex, _innerIndex);
        }
    }
}
