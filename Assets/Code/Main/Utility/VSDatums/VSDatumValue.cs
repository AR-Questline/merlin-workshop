using System;
using System.Runtime.InteropServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Enums;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VSDatums {
    [Serializable]
    public partial struct VSDatumValue {
        public ushort TypeForSerialization => SavedTypes.VSDatumValue;

        [SerializeField, Saved] SimpleValue simpleValue;
        [SerializeField, Saved] string stringValue;
        [SerializeField, Saved] ARAssetReference assetValue;

        public readonly bool Bool {
            get => simpleValue.boolValue;
            init => simpleValue = new SimpleValue(value);
        }
        public readonly int Int {
            get => simpleValue.intValue;
            init => simpleValue = new SimpleValue(value);
        }
        [UnityEngine.Scripting.Preserve] public readonly float Float {
            get => simpleValue.floatValue;
            init => simpleValue = new SimpleValue(value);
        }
        public readonly string String {
            get => stringValue;
            init => stringValue = value;
        }
        public readonly ARAssetReference Asset {
            get => assetValue;
            init => assetValue = value;
        }
        public readonly RichEnum RichEnum {
            get => RichEnum.Deserialize(stringValue);
            init => stringValue = value.Serialize();
        }

        public VSDatumValue Copy(in VSDatumType type) {
            return new VSDatumValue {
                simpleValue = simpleValue,
                stringValue = stringValue,
            };
        }

        public readonly void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer, in VSDatumType type) {
            jsonWriter.WriteStartObject();
            if (type.general == VSDatumGeneralType.String) {
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(stringValue), stringValue);
            } else if (type.general == VSDatumGeneralType.Asset) {
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(assetValue), assetValue);
            } else if (simpleValue.ShouldWriteSavables()) {
                jsonWriter.WritePropertyName(nameof(simpleValue));
                simpleValue.WriteSavables(jsonWriter, serializer);
            }
            jsonWriter.WriteEndObject();
        }
        
        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public partial struct SimpleValue {
            public ushort TypeForSerialization => SavedTypes.SimpleValue;

            [FieldOffset(0), NonSerialized] public readonly int intValue;
            [FieldOffset(0), NonSerialized] public readonly bool boolValue;
            [FieldOffset(0), NonSerialized] public readonly float floatValue;
            
            [FieldOffset(0), SerializeField, Saved] ulong rawValue;
            
            public SimpleValue(int value) : this() => intValue = value;
            public SimpleValue(bool value) : this() => boolValue = value;
            public SimpleValue(float value) : this() => floatValue = value;

            public readonly void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
                jsonWriter.WriteStartObject();
                JsonUtils.JsonWrite(jsonWriter, serializer, nameof(rawValue), rawValue);
                jsonWriter.WriteEndObject();
            }
            
            public readonly bool ShouldWriteSavables() => rawValue != 0;
        }
    }
}