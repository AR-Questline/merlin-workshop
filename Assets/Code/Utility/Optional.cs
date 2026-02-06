using System;

namespace Awaken.Utility {
    public struct Optional<T> {
        T _value;
        BlittableBool _hasValue;

        public bool HasValue => _hasValue;
        public T Value => _value;

        Optional(T value) {
            _value = value;
            _hasValue = true;
        }

        public static Optional<T> None => new Optional<T>();
        public static Optional<T> Some(T value) => new Optional<T>(value);
        public static Optional<T> NullChecked(T value) => value == null ? new Optional<T>() : new Optional<T>(value);

        public void Deconstruct(out bool hasValue, out T value) {
            hasValue = _hasValue;
            value = _value;
        }

        public readonly bool TryGetValue(out T value) {
            value = _value;
            return _hasValue;
        }

        public readonly T GetValueOrDefault(T defaultValue = default) {
            return _hasValue ? _value : defaultValue;
        }

        public readonly T GetValueOrThrow(string message = "Option does not have a value") {
            if (!_hasValue) {
                throw new InvalidOperationException(message);
            }
            return _value;
        }

        public readonly override string ToString() {
            return _hasValue ? $"Some({_value})" : "None";
        }

        public readonly override int GetHashCode() {
            return _hasValue ? _value.GetHashCode() : 0;
        }

        public static implicit operator Optional<T>(T value) {
            return new Optional<T>(value);
        }

        public static implicit operator bool(Optional<T> optional) {
            return optional._hasValue;
        }
    }
}
