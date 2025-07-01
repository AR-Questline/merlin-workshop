using UnityEngine;

namespace Awaken.Utility {
    public struct ForFrameValue<T> {
        int _frame;
        T _value;

        public T Value {
            get {
                if (_frame != Time.frameCount) {
                    _frame = Time.frameCount;
                    _value = default;
                }
                return _value;
            }
            set {
                _frame = Time.frameCount;
                _value = value;
            }
        }

        public static implicit operator T(ForFrameValue<T> value) {
            return value.Value;
        }
    }
}
