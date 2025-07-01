using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.Utility.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class DictionaryConverter : JsonConverter {
        public const string KeyTypeName = "keyType";
        public const string ValueTypeName = "valueType";
        public const string ValuesName = "values";
        public const string KeyName = "key";
        public const string ValueName = "value";
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Type type = value.GetType();
            IEnumerable keys = (IEnumerable)type.GetProperty("Keys")!.GetValue(value, null);
            IEnumerable values = (IEnumerable)type.GetProperty("Values")!.GetValue(value, null);
            IEnumerator valuesEnumerator = values.GetEnumerator();

            Type[] genericArguments = null;
            foreach (Type inter in type.GetInterfaces()) {
                if (inter.IsGenericType && inter.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
                    genericArguments = inter.GetGenericArguments();
                }
            }
            genericArguments ??= new[] {typeof(object), typeof(object)};
            
            Type keyType = genericArguments[0];
            Type valueType = genericArguments[1];
            writer.WriteStartObject();
            
            writer.WritePropertyName(KeyTypeName);
            writer.WriteValue(keyType.AssemblyQualifiedName);
            
            writer.WritePropertyName(ValueTypeName);
            writer.WriteValue(valueType.AssemblyQualifiedName);
            
            writer.WritePropertyName(ValuesName);

            writer.WriteStartArray();

            foreach (object k in keys) {
                valuesEnumerator.MoveNext();
                object val = valuesEnumerator.Current;
                if (val != null) {
                    writer.WriteStartObject();
                    writer.WritePropertyName(KeyName);
                    serializer.Serialize(writer, k);

                    writer.WritePropertyName(ValueName);
                    serializer.Serialize(writer, val);

                    writer.WriteEndObject();
                } else {
#if DEBUG || AR_DEBUG
                    throw new JsonWriterException($"Saving dictionary with null value is not supported. Key {k}; Path {writer.Path}");
#endif
                }
            }

            if (valuesEnumerator is IDisposable disposable) {
                disposable.Dispose();
            }
            
            writer.WriteEndArray();
            
            writer.WriteEndObject();

            /*writer.WriteStartArray();
            foreach (object key in keys) {
                valueEnumerator.MoveNext();

                writer.WriteStartArray();
                serializer.Serialize(writer, key);
                serializer.Serialize(writer, valueEnumerator.Current);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();*/
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            //Type[] genericArguments = objectType.GetGenericArguments();
            //Type keyType = genericArguments[0];
            //Type valueType = genericArguments[1];

            JObject jObject = JObject.Load(reader);
            Type keyType = jObject[KeyTypeName]!.ToObject<Type>();
            Type valueType = jObject[ValueTypeName]!.ToObject<Type>();
            
            if (objectType == typeof(IDictionary)) {
                objectType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            }

            IDictionary dictionary = (IDictionary)Activator.CreateInstance(objectType);

            JArray values = (JArray) jObject[ValuesName];
            foreach (var kvp in values!.OfType<JObject>()) {
                object key = kvp[KeyName]!.ToObject(keyType!, LoadSave.Get.serializer);
                object value = null;
                if (kvp.ContainsKey(ValueName)) {
                    value = kvp[ValueName]!.ToObject(valueType!, LoadSave.Get.serializer);
                }

                if (key != null) {
                    dictionary.Add(key, value);
                }
            }
            
            //JToken tokens = JToken.Load(reader);
            //Type keyType = tokens[0]!.ToObject<Type>();
            //Type valueType = tokens[1]!.ToObject<Type>();

           /* foreach (var eachToken in tokens.Skip(2)) {
                object key = eachToken[0]!.ToObject(keyType!, LoadSave.Get.serializer); 
                object value = eachToken[1]!.ToObject(valueType!, LoadSave.Get.serializer);
                if (key != null) {
                    dictionary.Add(key, value);
                }
            }*/

            return dictionary;
        }
        
        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(IDictionary), objectType);
        }

        // === Optimizations
        // Here are custom implementations of the same steps as in WriteJson, so if you made change to WriteJson
        // then adjust methods below
        
        // -- RelationStore
        static readonly string RelationAssemblyQualifiedName = typeof(Relation).AssemblyQualifiedName;
        static readonly string RelatedListAssemblyQualifiedName = typeof(List<IModel>).AssemblyQualifiedName;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomWriteJson(JsonWriter jsonWriter, JsonSerializer serializer, Dictionary<Relation, List<IModel>> dictionary) {
            jsonWriter.WriteStartObject();
            
            jsonWriter.WritePropertyName(DictionaryConverter.KeyTypeName);
            jsonWriter.WriteValue(RelationAssemblyQualifiedName);
            jsonWriter.WritePropertyName(DictionaryConverter.ValueTypeName);
            jsonWriter.WriteValue(RelatedListAssemblyQualifiedName);
            jsonWriter.WritePropertyName(DictionaryConverter.ValuesName);
            
            jsonWriter.WriteStartArray();
            foreach (var keyValuesPair in dictionary) {
                if (!keyValuesPair.Key.Pair.IsSaved) {
                    continue;
                }

                var relatedModels = keyValuesPair.Value;

                if (!ShouldSaveRelations(relatedModels)) {
                    continue;
                }

                jsonWriter.WriteStartObject();
                JsonUtils.JsonWrite(jsonWriter, serializer, DictionaryConverter.KeyName, keyValuesPair.Key);
                jsonWriter.WritePropertyName(DictionaryConverter.ValueName);
                jsonWriter.WriteStartArray();
                foreach (var model in relatedModels) {
                    if (model?.IsBeingSaved ?? false) {
                        ModelConverter.Write(jsonWriter, model);
                    }
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
            
            jsonWriter.WriteEndObject();
        }

        static bool ShouldSaveRelations(List<IModel> relatedModels) {
            for (var i = 0; i < relatedModels.Count; i++) {
                if (relatedModels[i]?.IsBeingSaved ?? false) {
                    return true;
                }
            }

            return false;
        }

        // -- AttachmentTracker
        static readonly string StringAssemblyQualifiedName = typeof(string).AssemblyQualifiedName;
        static readonly string ElementsSetAssemblyQualifiedName = typeof(HashSet<Element>).AssemblyQualifiedName;
        static readonly string TypesSetAssemblyQualifiedName = typeof(HashSet<Type>).AssemblyQualifiedName;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomWriteJson(JsonWriter jsonWriter, JsonSerializer serializer, MultiMap<string, Element> dictionary) {
            jsonWriter.WriteStartObject();
            
            jsonWriter.WritePropertyName(DictionaryConverter.KeyTypeName);
            jsonWriter.WriteValue(StringAssemblyQualifiedName);
            jsonWriter.WritePropertyName(DictionaryConverter.ValueTypeName);
            jsonWriter.WriteValue(ElementsSetAssemblyQualifiedName);
            jsonWriter.WritePropertyName(DictionaryConverter.ValuesName);
            
            jsonWriter.WriteStartArray();
            foreach (var keyValuesPair in dictionary) {
                jsonWriter.WriteStartObject();
                JsonUtils.JsonWrite(jsonWriter, serializer, DictionaryConverter.KeyName, keyValuesPair.Key);
                jsonWriter.WritePropertyName(DictionaryConverter.ValueName);
                jsonWriter.WriteStartArray();
                foreach (var element in keyValuesPair.Value) {
                    if (element?.IsBeingSaved ?? false) {
                        ModelConverter.Write(jsonWriter, element);
                    }
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
            
            jsonWriter.WriteEndObject();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomWriteJson(JsonWriter jsonWriter, JsonSerializer serializer, MultiMap<string, Type> dictionary) {
            jsonWriter.WriteStartObject();
            
            jsonWriter.WritePropertyName(DictionaryConverter.KeyTypeName);
            jsonWriter.WriteValue(StringAssemblyQualifiedName);
            jsonWriter.WritePropertyName(DictionaryConverter.ValueTypeName);
            jsonWriter.WriteValue(TypesSetAssemblyQualifiedName);
            jsonWriter.WritePropertyName(DictionaryConverter.ValuesName);
            
            jsonWriter.WriteStartArray();
            foreach (var keyValuesPair in dictionary) {
                jsonWriter.WriteStartObject();
                JsonUtils.JsonWrite(jsonWriter, serializer, DictionaryConverter.KeyName, keyValuesPair.Key);
                jsonWriter.WritePropertyName(DictionaryConverter.ValueName);
                jsonWriter.WriteStartArray();
                foreach (var value in keyValuesPair.Value) {
                    if (value != null) {
                        serializer.Serialize(jsonWriter, value);
                    }
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }
            
            jsonWriter.WriteEndArray();
            
            jsonWriter.WriteEndObject();
        }
    }
}