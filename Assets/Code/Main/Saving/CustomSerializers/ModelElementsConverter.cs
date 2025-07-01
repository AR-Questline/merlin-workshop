using System;
using System.Collections.Generic;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class ModelElementsConverter : JsonConverter {
        const string ElementsCountProperty = "Count";
        const string ElementsProperty = "Elements";

        public static void Write(JsonWriter writer, ModelElements modelElements) {
            var elements = ModelElements.Access.Elements(modelElements);
            var emptyElements = elements == null;

            writer.WriteStartObject();

            if (emptyElements == false) {
                var count = 0;
                foreach (var element in elements) {
                    if (element.IsBeingSaved) {
                        count++;
                    }
                }

                if (count > 0) {
                    writer.WritePropertyName(ElementsCountProperty);
                    writer.WriteValue(count);

                    writer.WritePropertyName(ElementsProperty);
                    writer.WriteStartArray();
                    foreach (var element in elements) {
                        if (element.IsBeingSaved) {
                            writer.WriteValue(ModelConverter.Convert(element));
                        }
                    }
                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Write(writer, (ModelElements)value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            try {
                if (reader.Read()) {
                    if (reader.TokenType == JsonToken.EndObject) {
                        return new ModelElements((List<Element>)null);
                    }

                    var propertyName = (string)reader.Value;
                    if (propertyName != ElementsCountProperty) {
                        throw JsonUtils.SerializationException(reader, $"Expected property name '{ElementsCountProperty}' but got '{propertyName}'.");
                    }
                    var count = reader.ReadAsInt32() ?? 0;

                    if (count < 1) {
                        return new ModelElements((List<Element>)null);
                    }

                    var elements = new List<Element>(count);

                    using var jsonReader = new JsonObjectReadScope(reader);

                    using (jsonReader.StartArrayScope(ElementsProperty)) {
                        for (var i = 0; i < count; i++) {
                            var elementId = jsonReader.ReadArrayElementString();
                            var element = ModelConverter.Convert<Element>(elementId);
                            if (element != null) {
                                elements.Add(element);
                            }
                        }
                    }

                    return new ModelElements(elements);
                }
                throw JsonUtils.SerializationException(reader, $"Expected {nameof(ModelElements)} object but failed read with token: '{reader.TokenType}'.");
            } catch (Exception e) {
                Debug.LogException(e);
                return new ModelElements((List<Element>)null);
            }
        }

        public override bool CanConvert(Type objectType) {
            return typeof(ModelElements) == objectType;
        }
    }
}
