using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class MatrixConverter : JsonConverter {
        public static void Write(JsonWriter writer, Matrix4x4 vector) {
            writer.WriteStartObject();
            
            writer.WritePropertyName("m00");
            writer.WriteValue(vector.m00);
            writer.WritePropertyName("m01");
            writer.WriteValue(vector.m01);
            writer.WritePropertyName("m02");
            writer.WriteValue(vector.m02);
            writer.WritePropertyName("m03");
            writer.WriteValue(vector.m03);
            
            writer.WritePropertyName("m10");
            writer.WriteValue(vector.m10);
            writer.WritePropertyName("m11");
            writer.WriteValue(vector.m11);
            writer.WritePropertyName("m12");
            writer.WriteValue(vector.m12);
            writer.WritePropertyName("m13");
            writer.WriteValue(vector.m13);
            
            writer.WritePropertyName("m20");
            writer.WriteValue(vector.m20);
            writer.WritePropertyName("m21");
            writer.WriteValue(vector.m21);
            writer.WritePropertyName("m22");
            writer.WriteValue(vector.m22);
            writer.WritePropertyName("m23");
            writer.WriteValue(vector.m23);
            
            writer.WritePropertyName("m30");
            writer.WriteValue(vector.m30);
            writer.WritePropertyName("m31");
            writer.WriteValue(vector.m31);
            writer.WritePropertyName("m32");
            writer.WriteValue(vector.m32);
            writer.WritePropertyName("m33");
            writer.WriteValue(vector.m33);

            writer.WriteEndObject();
        }
        
        public override void WriteJson(JsonWriter writer, object v, JsonSerializer serializer) {
            Matrix4x4 value = (Matrix4x4)v;
            Write(writer, value);
        }

        //CanRead is false which means the default implementation will be used instead.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return existingValue;
        }

        public override bool CanWrite => true;
        public override bool CanRead => false;
        
        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(Matrix4x4), objectType);
        }
    }
}