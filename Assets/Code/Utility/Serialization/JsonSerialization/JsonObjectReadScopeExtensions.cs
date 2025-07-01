using System;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.Utility.Serialization {
    public static class JsonObjectReadScopeExtensions {
        [UnityEngine.Scripting.Preserve]
        public static DateTime ReadPropertyDateTime(this JsonObjectReadScope scope, string expectedPropertyName = null, DateTime defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementDateTime(defaultValue);
        }

        public static DateTime ReadArrayElementDateTime(this JsonObjectReadScope scope, DateTime defaultValue = default) {
            return scope.reader.ReadAsDateTime() ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static bool ReadPropertyBool(this JsonObjectReadScope scope, string expectedPropertyName = null, bool defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementBool(defaultValue);
        }

        public static bool ReadArrayElementBool(this JsonObjectReadScope scope, bool defaultValue = default) {
            return scope.reader.ReadAsBoolean() ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static byte[] ReadPropertyByteArray(this JsonObjectReadScope scope, string expectedPropertyName = null, byte[] defaultValue = null) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementByteArray(defaultValue);
        }

        public static byte[] ReadArrayElementByteArray(this JsonObjectReadScope scope, byte[] defaultValue = default) {
            return scope.reader.ReadAsBytes() ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static decimal ReadPropertyDecimal(this JsonObjectReadScope scope, string expectedPropertyName = null, decimal defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementDecimal(defaultValue);
        }

        public static decimal ReadArrayElementDecimal(this JsonObjectReadScope scope, decimal defaultValue = default) {
            return scope.reader.ReadAsDecimal() ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static float ReadPropertyFloat(this JsonObjectReadScope scope, string expectedPropertyName = null, float defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementFloat(defaultValue);
        }

        public static float ReadArrayElementFloat(this JsonObjectReadScope scope, float defaultValue = default) {
            var value = scope.reader.ReadAsDouble() ?? defaultValue;
            return (float)value;
        }

        [UnityEngine.Scripting.Preserve]
        public static int ReadPropertyInt(this JsonObjectReadScope scope, string expectedPropertyName = null, int defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementInt(defaultValue);
        }

        public static int ReadArrayElementInt(this JsonObjectReadScope scope, int defaultValue = default) {
            var value = scope.reader.ReadAsInt32() ?? defaultValue;
            return value;
        }

        public static uint ReadPropertyUInt(this JsonObjectReadScope scope, string expectedPropertyName = null, uint defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementUInt(defaultValue);
        }

        public static uint ReadArrayElementUInt(this JsonObjectReadScope scope, uint defaultValue = default) {
            var value = scope.reader.ReadAsInt32() ?? (int)defaultValue;
            return (uint)value;
        }

        [UnityEngine.Scripting.Preserve]
        public static short ReadPropertyShort(this JsonObjectReadScope scope, string expectedPropertyName = null, short defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementShort(defaultValue);
        }

        public static short ReadArrayElementShort(this JsonObjectReadScope scope, short defaultValue = default) {
            int intValue = scope.reader.ReadAsInt32() ?? defaultValue;
            if ((intValue > short.MaxValue) | (intValue < short.MinValue)) {
                Log.Important?.Error($"written value {intValue} does not fit into int16 range [{short.MinValue}, {short.MaxValue}]");
                return defaultValue;
            }

            return (short)intValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static ushort ReadPropertyUShort(this JsonObjectReadScope scope, string expectedPropertyName = null, ushort defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementUShort(defaultValue);
        }

        public static ushort ReadArrayElementUShort(this JsonObjectReadScope scope, ushort defaultValue = default) {
            int intValue = scope.reader.ReadAsInt32() ?? defaultValue;
            if ((intValue > ushort.MaxValue) | (intValue < ushort.MinValue)) {
                Log.Important?.Error($"written value {intValue} does not fit into uint16 range [{ushort.MinValue}, {ushort.MaxValue}]");
                return defaultValue;
            }

            return (ushort)intValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static sbyte ReadPropertySByte(this JsonObjectReadScope scope, string expectedPropertyName = null, sbyte defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementSByte(defaultValue);
        }

        public static sbyte ReadArrayElementSByte(this JsonObjectReadScope scope, sbyte defaultValue = default) {
            int intValue = scope.reader.ReadAsInt32() ?? defaultValue;
            if ((intValue > sbyte.MaxValue) | (intValue < sbyte.MinValue)) {
                Log.Important?.Error($"written value {intValue} does not fit into int16 range [{sbyte.MinValue}, {sbyte.MaxValue}]");
                return defaultValue;
            }

            return (sbyte)intValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static byte ReadPropertyByte(this JsonObjectReadScope scope, string expectedPropertyName = null, byte defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementByte(defaultValue);
        }

        public static byte ReadArrayElementByte(this JsonObjectReadScope scope, byte defaultValue = default) {
            int intValue = scope.reader.ReadAsInt32() ?? defaultValue;
            if ((intValue > byte.MaxValue) | (intValue < byte.MinValue)) {
                Log.Important?.Error($"written value {intValue} does not fit into uint16 range [{byte.MinValue}, {byte.MaxValue}]");
                return defaultValue;
            }

            return (byte)intValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static string ReadPropertyString(this JsonObjectReadScope scope, string expectedPropertyName = null, string defaultValue = null) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementString(defaultValue);
        }

        public static string ReadArrayElementString(this JsonObjectReadScope scope, string defaultValue = default) {
            return scope.reader.ReadAsString() ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public static ulong ReadPropertyULong(this JsonObjectReadScope scope, string expectedPropertyName = null, ulong defaultValue = default) {
            if (TryGetDefaultValueIfNameNotMatching(scope.reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            return scope.ReadArrayElementUlong(defaultValue);
        }

        public static ulong ReadArrayElementUlong(this JsonObjectReadScope scope, ulong defaultValue = default) {
            // Need to parse like this because automatic conversion from object converts big ulong values to incorrect type
            var valueString = scope.reader.ReadAsString() ?? "null";
            if (ulong.TryParse(valueString, out var value)) {
                return value;
            }

            Log.Important?.Error($"Cannot parse {valueString} as ulong");
            return defaultValue;
        }

        internal static bool TryGetDefaultValueIfNameNotMatching<T>(JsonReader reader, string expectedPropertyName, T defaultValue, out T value) {
            reader.Read();
            var propertyName = (string)reader.Value;
            if (PropertyNameDoMatch(reader, expectedPropertyName, propertyName) == false) {
                reader.Read();
                value = defaultValue;
                return true;
            }

            value = default;
            return false;
        }

        internal static bool PropertyNameDoMatch(JsonReader reader, string expectedPropertyName, string propertyName) {
            if (expectedPropertyName != null && propertyName != expectedPropertyName) {
                Log.Important?.Error($"Json serialization error. Expected property name is {expectedPropertyName} but read property name is {propertyName}");
                // Skip value
                reader.Read();
                return false;
            }

            return true;
        }
    }
}