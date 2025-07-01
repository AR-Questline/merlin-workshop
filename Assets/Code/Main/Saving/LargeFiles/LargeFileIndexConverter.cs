using System;
using Awaken.TG.MVC;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Saving.LargeFiles {
    public class LargeFileIndexConverter : JsonConverter {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var fileIndex = ((LargeFileIndex)value).value;
            World.Services.Get<LargeFilesStorage>().AddUsedLargeFile(fileIndex);
            writer.WriteValue(fileIndex);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            int intValue = Convert.ToInt32(reader.Value);
            return new LargeFileIndex(intValue);
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(LargeFileIndex);
        }
    }
}