using System;
using Awaken.Utility.LowLevel.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.Utility.CustomSerializers {
    public class FrugalListConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteStartObject();
            var frugalList = (IFrugalList)value;
            var backingElement = frugalList!.BackingElement;
            if (backingElement == null) {
                writer.WriteEndObject();
                return;
            }
            var backingElementType = backingElement.GetType();
            if (backingElementType.IsGenericType && backingElementType.GetGenericTypeDefinition() == typeof(UnsafePinnableList<>)) {
                writer.WritePropertyName("MultipleObjects");
            } else {
                writer.WritePropertyName("SingleObject");
            }
            serializer.Serialize(writer, backingElement, typeof(object));
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            existingValue ??= Activator.CreateInstance(objectType);
            JObject jObject = JObject.Load(reader);
            var a = jObject["SingleObject"];
            if (a != null) {
                var bakingElement = a.ToObject(objectType.GetGenericArguments()[0], serializer);
                ((IFrugalList)existingValue)!.BackingElement = bakingElement;
                return existingValue;
            }
            var b = jObject["MultipleObjects"];
            if (b != null) {
                var bakingElement = b.ToObject(typeof(UnsafePinnableList<>).MakeGenericType(objectType.GetGenericArguments()), serializer);
                ((IFrugalList)existingValue)!.BackingElement = bakingElement;
            }
            return existingValue;
        }

        public override bool CanConvert(Type objectType) {
            return typeof(IFrugalList).IsAssignableFrom(objectType);
        }
    }
}