using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Serialization;
using Newtonsoft.Json;
using Unity.Collections;

namespace Awaken.Utility.LowLevel.Collections {
    public class UnsafeSparseBitmaskConverter : JsonConverter {
        const string RangesCountProperty = "rangesCount";
        const string RangedBucketsCountProperty = "rangedBucketsCount";
        const string RangesProperty = "ranges";
        const string RangedBucketsProperty = "rangedBuckets";

        public override unsafe void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var bitMask = (UnsafeSparseBitmask)value;

            if (bitMask.IsCreated && bitMask._allocator != Allocator.Persistent) {
                throw new NotSupportedException($"Serializing unmanaged data with allocator {bitMask._allocator} is not supported because it can create hard to track bugs after deserialization. Only {nameof(Allocator.Persistent)} is supported");
            }

            writer.WriteStartObject();

            writer.WritePropertyName(RangesCountProperty);
            writer.WriteValue(bitMask._rangesCount);

            writer.WritePropertyName(RangedBucketsCountProperty);
            writer.WriteValue(bitMask._rangedBucketsCount);
            
            writer.WritePropertyName(RangesProperty);
            writer.WriteStartArray();
            for (int i = 0; i < bitMask._rangesCount; i++) {
                writer.WriteValue(bitMask._ranges[i].startBucketIndex);
                writer.WriteValue(bitMask._ranges[i].bucketsCount);
            }

            writer.WriteEndArray();

            writer.WritePropertyName(RangedBucketsProperty);
            writer.WriteStartArray();
            for (int i = 0; i < bitMask._rangedBucketsCount; i++) {
                writer.WriteValue(bitMask._rangedBuckets[i]);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override unsafe object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            using var readScope = new JsonObjectReadScope(reader);

            uint rangesCount = readScope.ReadPropertyUInt(RangesCountProperty);
            uint rangedBucketsCount = readScope.ReadPropertyUInt(RangedBucketsCountProperty);
            
            var bitMask = new UnsafeSparseBitmask(ARAlloc.Persistent, rangesCount, rangedBucketsCount);

            bitMask._rangesCount = rangesCount;
            bitMask._rangedBucketsCount = rangedBucketsCount;

            using (readScope.StartArrayScope(RangesProperty)) {
                for (int i = 0; i < rangesCount; i++) {
                    var startBucketIndex = readScope.ReadArrayElementUShort();
                    var bucketsCount = readScope.ReadArrayElementUShort();
                    bitMask._ranges[i] = new UnsafeSparseBitmask.MaskRange(startBucketIndex, bucketsCount);
                }
            }

            using (readScope.StartArrayScope(RangedBucketsProperty)) {
                for (int i = 0; i < rangedBucketsCount; i++) {
                    bitMask._rangedBuckets[i] = readScope.ReadArrayElementUlong();
                }
            }

            return bitMask;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(UnsafeSparseBitmask);
        }
    }
}