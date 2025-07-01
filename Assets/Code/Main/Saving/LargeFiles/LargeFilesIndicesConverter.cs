using System;
using Awaken.TG.MVC;
using Awaken.Utility.LowLevel.Collections;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Saving.LargeFiles {
    public class LargeFilesIndicesConverter : JsonConverter {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var bitmask = ((LargeFilesIndices)value).value;
            World.Services.Get<LargeFilesStorage>().AddUsedLargeFiles(in bitmask);
            serializer.Serialize(writer, bitmask);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var bitmask = serializer.Deserialize<UnsafeBitmask>(reader);
            return new LargeFilesIndices(bitmask);
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(LargeFilesIndices);
        }
    }
}