using System;

namespace UnityEngine.Rendering {
    static class ToIntEnumHelper {
        public static int ToInt(sbyte value) { return value; }
        public static int ToInt(byte value) { return value; }
        public static int ToInt(short value) { return value; }
        public static int ToInt(ushort value) { return value; }
        public static int ToInt(int value) { return value; }
        public static int ToInt(uint value) { return (int)value; }
    }

    static class ToIntEnumHelper<TEnum> where TEnum : Enum {
        public static readonly Func<TEnum, int> ToIntDelegate = CreateToInt();

        static Func<TEnum, int> CreateToInt() {
            var underlyingType = typeof(TEnum).GetEnumUnderlyingType();
            var method = typeof(ToIntEnumHelper).GetMethod(nameof(ToIntEnumHelper.ToInt), new[] { underlyingType });
            if (method == null) {
                return InvalidConversion;
            }
            return (Func<TEnum, int>)Delegate.CreateDelegate(typeof(Func<TEnum, int>), method);
        }

        static int InvalidConversion(TEnum _) {
            throw new InvalidCastException();
        }
    }
}
