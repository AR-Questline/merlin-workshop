namespace Awaken.Utility.Collections {
    /// <summary>
    /// Non thread safe tracking of an array of values. Will result in the order of the array being changed.
    /// </summary>
    public struct RemovableSpan<T> {
        readonly T[] _array;
        public int length;

        public RemovableSpan(ref T[] array) {
            this._array = array;
            length = array.Length;
        }
            
        public void RemoveAtSwapBack(int index) {
            (_array[index], _array[length - 1]) = (_array[length - 1], _array[index]);
            length--;
        }
            
        public T this[int index] {
            get => _array[index];
            set => _array[index] = value;
        }
    }
}