using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Serialization;
using Newtonsoft.Json;
using Unity.Collections;

namespace Awaken.Utility.LowLevel.Collections {
    public class UnsafeBitmaskConverter : JsonConverter {
        const string ElementsCountProperty = "elementsLength";
        const string MasksProperty = "masks";
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override unsafe void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var bitMask = (UnsafeBitmask)value;
            if (bitMask.IsCreated == false) {
                writer.WriteStartObject();
            
                writer.WritePropertyName(ElementsCountProperty);
                writer.WriteValue(0);
                
                writer.WriteEndObject();
                return;
            }
            if (bitMask._allocator != Allocator.Persistent) {
                throw new NotSupportedException($"Serializing unmanaged data with allocator {bitMask._allocator} is not supported because it can create hard to track bugs after deserialization. Only {nameof(Allocator.Persistent)} is supported");
            }
            writer.WriteStartObject();
            
            writer.WritePropertyName(ElementsCountProperty);
            writer.WriteValue(bitMask._elementsLength);
            // Write the masks array
            writer.WritePropertyName(MasksProperty);
            writer.WriteStartArray();
            for (int i = 0; i < bitMask.BucketsLength; i++) {
                writer.WriteValue(bitMask._masks[i]);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override unsafe object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            using var readScope = new JsonObjectReadScope(reader);
            
            uint elementsCount = readScope.ReadPropertyUInt(ElementsCountProperty);
            if (elementsCount == 0) {
                return default(UnsafeBitmask);
            }
            var bitMask = new UnsafeBitmask(elementsCount, ARAlloc.Persistent);

            var bucketsCount = bitMask.BucketsLength;

            using (readScope.StartArrayScope(MasksProperty)) {
                for (int i = 0; i < bucketsCount; i++) {
                    var maskValue = readScope.ReadArrayElementUlong();
                    bitMask._masks[i] = maskValue;
                }
            }
            
            return bitMask;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(UnsafeBitmask);
        }
    }
}