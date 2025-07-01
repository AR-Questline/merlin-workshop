using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Saving.CustomSerializers;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.Utility.Enums;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using FMODUnity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving {
    public static class JsonUtils {
        public static readonly Vector3 VectorZero = Vector3.zero;
        public static readonly Quaternion QuaternionIdentity = Quaternion.identity;
        public static readonly Matrix4x4 MatrixIdentity = Matrix4x4.identity;
        
        /// <summary>
        /// Adds to given JObject property (name, value) from object (obj)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static void AddToJObject<T>(JObject jObject, JsonSerializer serializer, string name, T value, object obj) {
#if DEBUG
            AddToJObject_Debug(jObject, serializer, name, value, obj);
#else
            if (value == null) {
                return;
            }

            JToken newToken = JToken.FromObject(value, serializer);
            jObject.Add(name, newToken);
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, object value, Type baseType = null) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            serializer.Serialize(jsonWriter, value, baseType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, bool value, bool defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, string value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, int value, int defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ulong value, ulong defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, float value, float defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, float? value, float? defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, long value, long defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, double value, double defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, double? value, double defaultValue = default) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Vector3 value) {
            if (value == VectorZero) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            Vector3Converter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Vector3 value, Vector3 defaultValue) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            Vector3Converter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Quaternion value) {
            if (value == QuaternionIdentity) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            QuaternionConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Quaternion value, Quaternion defaultValue) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            QuaternionConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Matrix4x4 value) {
            if (value == MatrixIdentity) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            MatrixConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Matrix4x4 value, Matrix4x4 defaultValue) {
            if (value == defaultValue) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            MatrixConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ITemplate value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            TemplateConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ItemTemplate value) {
            if (value == null) {
                return;
            }

            jsonWriter.WritePropertyName(name);
            TemplateConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, IModel value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            ModelConverter.Write(jsonWriter, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Model value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            ModelConverter.Write(jsonWriter, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, RichEnum value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            RichEnumConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ARAssetReference value) {
            if (value == null || !value.IsSet) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            AssetReferenceConverter.Write(jsonWriter, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ShareableARAssetReference value) {
            if (value == null || !value.IsSet) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            AssetReferenceConverter.Write(jsonWriter, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, TemplateReference value) {
            if (value == null || !value.IsSet) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, RelationStore value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, Stat value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] [UnityEngine.Scripting.Preserve]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, SkillVariable value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, in VSDatumType value) {
            if (value.Equals(default)) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, in VSDatumValue value, in VSDatumType type) {
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer, type);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, BlendShapesFeature value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ItemSpawningDataRuntime value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, StoryBookmark value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite<T>(JsonWriter jsonWriter, JsonSerializer serializer, string name, FrugalList<T> value) {
            if (value.Count == 0) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            serializer.Serialize(jsonWriter, value, value.GetType());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ModelElements value) {
            jsonWriter.WritePropertyName(name);
            ModelElementsConverter.Write(jsonWriter, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, ItemInSlots value) {
            jsonWriter.WritePropertyName(name);
            ItemInSlotsConverter.Write(jsonWriter, value, serializer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, AliveAudioContainer value) {
            jsonWriter.WritePropertyName(name);
            value.WriteSavables(jsonWriter, serializer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, EventReference value) {
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(EventReference.Guid), value.Guid);
            jsonWriter.WriteEndObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, FMOD.GUID value) {
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteStartObject();
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(FMOD.GUID.Data1), value.Data1);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(FMOD.GUID.Data2), value.Data2);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(FMOD.GUID.Data3), value.Data3);
            JsonUtils.JsonWrite(jsonWriter, serializer, nameof(FMOD.GUID.Data4), value.Data4);
            jsonWriter.WriteEndObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite<T>(JsonWriter jsonWriter, JsonSerializer serializer, string name, T value) where T : Enum {
            var intValue = value.ToInt();
            if (intValue == 0) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteValue(intValue);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite<TWrapper, TSource>(JsonWriter jsonWriter, JsonSerializer serializer, string name, 
            in TWrapper wrapper, TSource source) where TWrapper : INestedJsonWrapper<TSource>  {
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteStartObject();
            wrapper.WriteSavables(source, jsonWriter, serializer);
            jsonWriter.WriteEndObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, LocationInitializer value, Type baseType = null) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteStartObject();
            WriteTypeIfNeeded(jsonWriter, baseType, value);
            value.WriteSavables(jsonWriter, serializer);
            jsonWriter.WriteEndObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, string[] value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteStartArray();
            foreach (var s in value) {
                jsonWriter.WriteValue(s);
            }
            jsonWriter.WriteEndArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JsonWrite(JsonWriter jsonWriter, JsonSerializer serializer, string name, List<string> value) {
            if (value == null) {
                return;
            }
            jsonWriter.WritePropertyName(name);
            jsonWriter.WriteStartArray();
            foreach (var s in value) {
                jsonWriter.WriteValue(s);
            }
            jsonWriter.WriteEndArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteTypeIfNeeded(JsonWriter jsonWriter, Type baseType, object value) {
            if (baseType is null) {
                return;
            }
            var type = value.GetType();
            if (type == baseType) {
                return;
            }
            jsonWriter.WritePropertyName("$type");
            jsonWriter.WriteValue(JsonTypeName.Get(type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AddToJObject_Debug<T>(JObject jObject, JsonSerializer serializer, string name, T value, object obj) {
            if (value == null) {
                return;
            }

            try {
                JToken newToken = JToken.FromObject(value, serializer);
                jObject.Add(name, newToken);
            } catch (TargetInvocationException e) {
                throw new SerializationException($"Cannot serialize property {name} in {obj}. Error {e.InnerException} {e.Message} {e.Data} {e.Source} {e.StackTrace}");
            } catch (Exception e) {
                throw new SerializationException($"Cannot serialize property {name} in {obj}. Error ({e.GetType().FullName}): {e.Message}");
            }
        }

        public static JsonSerializationException SerializationException(JsonReader reader, string message) {
            var lineInfo = reader as IJsonLineInfo;
            var path = reader.Path;

            message = FormatMessage(lineInfo, path, message);

            int lineNumber;
            int linePosition;
            if (lineInfo != null && lineInfo.HasLineInfo()) {
                lineNumber = lineInfo.LineNumber;
                linePosition = lineInfo.LinePosition;
            } else {
                lineNumber = 0;
                linePosition = 0;
            }

            return new JsonSerializationException(message, path, lineNumber, linePosition, null);

            static string FormatMessage(IJsonLineInfo lineInfo, string path, string message) {
                if (!message.EndsWith(Environment.NewLine, StringComparison.Ordinal)) {
                    message = message.Trim();

                    if (!message.EndsWith('.')) {
                        message += ".";
                    }

                    message += " ";
                }

                message += $"Path '{path}'";

                if (lineInfo != null && lineInfo.HasLineInfo()) {
                    message += $", line {lineInfo.LineNumber}, position {lineInfo.LinePosition}";
                }

                message += ".";

                return message;
            }
        }
    }
}
