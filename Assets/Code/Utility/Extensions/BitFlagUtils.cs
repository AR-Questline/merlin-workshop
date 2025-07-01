using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace Awaken.Utility.Extensions {
    public static class EnumExtension {
        // -- HasFlag
        static class HasFlagFastEnumHelper<T1>
        {
            public static Func<T1, T1, bool> testOverlapProc = InitProc;
            public static bool Contains(sbyte p1, sbyte p2) { return (p1 & p2) == p2; }
            public static bool Contains(byte p1, byte p2) { return (p1 & p2) == p2; }
            public static bool Contains(short p1, short p2) { return (p1 & p2) == p2; }
            public static bool Contains(ushort p1, ushort p2) { return (p1 & p2) == p2; }
            public static bool Contains(int p1, int p2) { return (p1 & p2) == p2; }
            public static bool Contains(uint p1, uint p2) { return (p1 & p2) == p2; }
            public static bool InitProc(T1 p1, T1 p2)
            {
                Type typ1 = typeof(T1);
                if (typ1.IsEnum) typ1 = Enum.GetUnderlyingType(typ1);
                Type[] types = { typ1, typ1 };
                var method = typeof(HasFlagFastEnumHelper<T1>).GetMethod("Contains", types);
                if (method == null) throw new MissingMethodException("Unknown type of enum");
                testOverlapProc = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
                return testOverlapProc(p1, p2);
            }
        }
        
        [Pure]
        public static bool HasFlagFast<T>(this T p1, T p2) where T : Enum {
            return HasFlagFastEnumHelper<T>.testOverlapProc(p1, p2);
        }
        
        // -- HasAnyFlag
        static class HasCommonBitsEnumHelper<T1>
        {
            public static Func<T1, T1, bool> testOverlapProc = InitProc;
            public static bool Overlaps(sbyte p1, sbyte p2) { return (p1 & p2) != 0; }
            public static bool Overlaps(byte p1, byte p2) { return (p1 & p2) != 0; }
            public static bool Overlaps(short p1, short p2) { return (p1 & p2) != 0; }
            public static bool Overlaps(ushort p1, ushort p2) { return (p1 & p2) != 0; }
            public static bool Overlaps(int p1, int p2) { return (p1 & p2) != 0; }
            public static bool Overlaps(uint p1, uint p2) { return (p1 & p2) != 0; }
            public static bool InitProc(T1 p1, T1 p2)
            {
                Type typ1 = typeof(T1);
                if (typ1.IsEnum) typ1 = Enum.GetUnderlyingType(typ1);
                Type[] types = { typ1, typ1 };
                var method = typeof(HasCommonBitsEnumHelper<T1>).GetMethod("Overlaps", types);
                if (method == null) throw new MissingMethodException("Unknown type of enum");
                testOverlapProc = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
                return testOverlapProc(p1, p2);
            }
        }

        [Pure]
        public static bool HasCommonBitsFast<T>(this T p1, T p2) where T : Enum {
            return HasCommonBitsEnumHelper<T>.testOverlapProc(p1, p2);
        }

        // -- ToString
        static class ToStringEnumHelper<T1> {
            static Dictionary<T1, string> _cache;
            
            static ToStringEnumHelper() {
                _cache = new Dictionary<T1, string>();
            }
            
            public static string ToString(T1 p1)
            {
                if (_cache.TryGetValue(p1, out string toStringValue)) {
                    return toStringValue;
                }
                
                toStringValue = p1.ToString();
                _cache[p1] = toStringValue;
                return toStringValue;
            }
        }
        
        public static string ToStringFast<T>(this T p1) where T : Enum
        {
            return ToStringEnumHelper<T>.ToString(p1);
        }
        
        // -- ToInt
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
        
        public static int ToInt<TEnum>(this TEnum value) where TEnum : Enum {
            return ToIntEnumHelper<TEnum>.ToIntDelegate(value);
        }
        
        // -- ToEnum
        static class ToEnumHelper<TSource, TEnum> where TEnum : Enum {
            public static readonly Func<TSource, TEnum> ToEnumDelegate = CreateToEnum();
            
            static Func<TSource, TEnum> CreateToEnum() {
                try {
                    var sourceType = typeof(TSource);
                    var enumType = typeof(TEnum);
                    var underlyingType = enumType.GetEnumUnderlyingType();

                    var intParameter = Expression.Parameter(sourceType);
                    var underlyingCast = Expression.Convert(intParameter, underlyingType);
                    var enumCast = Expression.Convert(underlyingCast, enumType);
                    return Expression.Lambda<Func<TSource, TEnum>>(enumCast, intParameter).Compile();
                } catch {
                    return InvalidConversion;
                }
            }

            static TEnum InvalidConversion(TSource source) {
                throw new InvalidCastException($"Cannot cast {typeof(TSource).Name} {source} to {typeof(TEnum).Name}");
            }
        }
        
        public static TEnum ToEnum<TEnum>(this sbyte value) where TEnum : Enum => ToEnumHelper<sbyte, TEnum>.ToEnumDelegate(value);
        public static TEnum ToEnum<TEnum>(this byte value) where TEnum : Enum => ToEnumHelper<byte, TEnum>.ToEnumDelegate(value);
        public static TEnum ToEnum<TEnum>(this short value) where TEnum : Enum => ToEnumHelper<short, TEnum>.ToEnumDelegate(value);
        public static TEnum ToEnum<TEnum>(this ushort value) where TEnum : Enum => ToEnumHelper<ushort, TEnum>.ToEnumDelegate(value);
        public static TEnum ToEnum<TEnum>(this int value) where TEnum : Enum => ToEnumHelper<int, TEnum>.ToEnumDelegate(value);
        public static TEnum ToEnum<TEnum>(this uint value) where TEnum : Enum => ToEnumHelper<uint, TEnum>.ToEnumDelegate(value);
        public static Enum ToEnum(this int value, Type enumType) {
            try {
                var sourceType = typeof(int);
                var underlyingType = enumType.GetEnumUnderlyingType();

                var intParameter = Expression.Parameter(sourceType);
                var underlyingCast = Expression.Convert(intParameter, underlyingType);
                var enumCast = Expression.Convert(underlyingCast, enumType);
                var enumEnumCast = Expression.Convert(enumCast, typeof(Enum));
                return Expression.Lambda<Func<int, Enum>>(enumEnumCast, intParameter).Compile()(value);
            } catch {
                return default;
            }
        }
    }

    [UnityEngine.Scripting.Preserve]
    public static class EnumUtils {
        public static TEnum LastValue<TEnum>() where TEnum : Enum {
            var array = Enum.GetValues(typeof(TEnum));
            var value = array.GetValue(array.Length - 1);
            return (TEnum) value;
        }
    }

    public static class BitFlagUtils {
        /// <summary>
        /// Does bit array p1 contain all bits in bit array p2. For safety use the correct types
        /// </summary>
        public static bool ContainsBits(long p1, long p2) {
            return (p1 & p2) == p1;
        }
        /// <inheritdoc cref="ContainsBits(long,long)"/>
        public static bool ContainsBits(uint p1, uint p2) {
            return (p1 & p2) == p1;
        }
        /// <inheritdoc cref="ContainsBits(long,long)"/>
        public static bool ContainsBits(int p1, int p2) {
            return (p1 & p2) == p1;
        }

        // from: https://stackoverflow.com/a/1333267
        public static int GetSetBitCount(long enumVal) {
            int iCount = 0;

            //Loop the value while there are still bits
            while (enumVal != 0)
            {
                //Remove the end bit
                enumVal &= (enumVal - 1);

                //Increment the count
                iCount++;
            }

            //Return the count
            return iCount;
        }

        /// <summary>
        ///   -1: No set bit in enum
        /// 0-63: pos of first set bit
        /// </summary>
        public static int GetPositionOfFirstSetBit(long enumVal) {
            if (enumVal == 0L) return -1; // There is no set flag in a 0
            int pos = 0;
 
            // Moves a 1 from position 0 upwards until the | (or) does not change the enum. (pos is at position of a 1)
            while ((enumVal | (1L << pos)) != enumVal) 
                ++pos;

            return pos;
        }

        /// <summary>
        ///   -1: No set bit in enum
        /// 0-63: pos of last set bit
        /// </summary>
        public static int GetPositionOfLastSetBit(long enumVal) {
            if (enumVal == 0L) return -1; // There is no set flag in a 0
            int pos = 63;

            // Moves a 1 from position 63 (last long bit) downwards until the | (or) does not change the enum. (pos is at position of a 1)
            while ((enumVal | (1L << pos)) != enumVal) 
                --pos;

            return pos;
        }
    }
}