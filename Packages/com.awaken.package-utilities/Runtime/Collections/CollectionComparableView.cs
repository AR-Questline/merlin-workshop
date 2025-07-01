using System;

namespace Awaken.PackageUtilities.Collections {
    public readonly unsafe struct CollectionComparableView<T> : IEquatable<CollectionComparableView<T>> where T : unmanaged, IEquatable<T> {
        readonly T* _ptr;
        readonly int _length;

        public T* Ptr => _ptr;
        public int Length => _length;
        public ref readonly T this[int index] => ref _ptr[index];
        
        public CollectionComparableView(T* ptr, int length) {
            _ptr = ptr;
            _length = length;
        }
        
        public bool Equals(CollectionComparableView<T> other) {
            if (_length != other._length) {
                return false;
            }
            for (int i = 0; i < _length; i++) {
                if (!_ptr[i].Equals(other._ptr[i])) {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) {
            return obj is CollectionComparableView<T> other && Equals(other);
        }

        public override int GetHashCode() {
            int hash = 0;
            for (int i = 0; i < _length; i++) {
                hash = hash * 397 + _ptr[i].GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(CollectionComparableView<T> left, CollectionComparableView<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(CollectionComparableView<T> left, CollectionComparableView<T> right) {
            return !left.Equals(right);
        }
    }
}