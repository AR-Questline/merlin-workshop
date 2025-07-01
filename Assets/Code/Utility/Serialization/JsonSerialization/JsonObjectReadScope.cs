using System;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.Utility.Serialization {
    public struct JsonObjectReadScope : IDisposable {
        public JsonReader reader;

        public JsonObjectReadScope(JsonReader reader) {
            this.reader = reader;
        }

        [UnityEngine.Scripting.Preserve]
        public T ReadProperty<T>(Func<T?> readFunc, string expectedPropertyName = null, T defaultValue = default) where T : struct {
            if (JsonObjectReadScopeExtensions.TryGetDefaultValueIfNameNotMatching(reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            T? value = readFunc.Invoke();
            return value ?? default;
        }

        [UnityEngine.Scripting.Preserve]
        public T ReadProperty<T>(Func<T> readFunc, string expectedPropertyName = null, T defaultValue = null) where T : class {
            if (JsonObjectReadScopeExtensions.TryGetDefaultValueIfNameNotMatching(reader, expectedPropertyName, defaultValue, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            T value = readFunc.Invoke();
            return value ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public T ReadArrayElement<T>(Func<T?> readFunc) where T : struct {
            T? value = readFunc.Invoke();
            return value ?? default;
        }

        [UnityEngine.Scripting.Preserve]
        public T ReadArrayElement<T>(Func<T> readFunc, T defaultValue = null) where T : class {
            T value = readFunc.Invoke();
            return value ?? defaultValue;
        }

        [UnityEngine.Scripting.Preserve]
        public object ReadProperty(string expectedPropertyName = null) {
            if (JsonObjectReadScopeExtensions.TryGetDefaultValueIfNameNotMatching<object>(reader, expectedPropertyName, null, out var resultDefaultValue)) {
                return resultDefaultValue;
            }

            reader.Read();
            return reader.Value;
        }

        [UnityEngine.Scripting.Preserve]
        public object ReadArrayElement() {
            reader.Read();
            return reader.Value;
        }

        public ArrayScope StartArrayScope(string expectedArrayName = null) {
            return new ArrayScope(reader, expectedArrayName);
        }

        public void Dispose() {
            this.reader.Read();
            if (reader.TokenType != JsonToken.EndObject) {
                Log.Important?.Error($"Json serialization error. Expected json token {nameof(JsonToken.EndObject)} but read {reader.TokenType}");
            }
        }

        public struct ArrayScope : IDisposable {
            public JsonReader reader;

            public ArrayScope(JsonReader reader, string expectedArrayName) {
                this.reader = reader;
                StartReadingArray(expectedArrayName);
            }

            void StartReadingArray(string expectedArrayName) {
                reader.Read();
                var arrayName = (string)reader.Value;
                if (JsonObjectReadScopeExtensions.PropertyNameDoMatch(reader, expectedArrayName, arrayName) == false) {
                    Log.Important?.Error($"Json serialization error. Expected array name is {expectedArrayName} but read array name is {arrayName}");
                }

                reader.Read();
                if (reader.TokenType != JsonToken.StartArray) {
                    Log.Important?.Error($"Json serialization error. Expected json token {nameof(JsonToken.StartArray)} but read {reader.TokenType}");
                }
            }

            public void Dispose() {
                // Read end array
                reader.Read();
                if (reader.TokenType != JsonToken.EndArray) {
                    Log.Important?.Error($"Json serialization error. Expected json token {nameof(JsonToken.EndArray)} but read {reader.TokenType}");
                }
            }
        }
    }
}