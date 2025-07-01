using System;

namespace Awaken.TG.Main.General {
    [Serializable]
    public struct ConditionalValue<T> where T : struct {
        public bool enable;
        public T value;

        public ConditionalValue(bool enable, T value) {
            this.enable = enable;
            this.value = value;
        }

        public ConditionalValue(T value) : this() {
            enable = true;
            this.value = value;
        }

        public static implicit operator bool(ConditionalValue<T> conditional) => conditional.enable;
        public static implicit operator T?(ConditionalValue<T> conditional) => conditional.enable ? (T?)conditional.value : null;
        public static implicit operator ConditionalValue<T>(T? nullableVal) => nullableVal.HasValue ? new ConditionalValue<T>(nullableVal.Value) : new ConditionalValue<T>();
        public static explicit operator T(ConditionalValue<T> conditionalInt) => conditionalInt.value;
        public static explicit operator ConditionalValue<T>(T intValue) => new ConditionalValue<T>(intValue);

        public override string ToString() {
            return enable ? value.ToString() : enable.ToString();
        }
    }
}