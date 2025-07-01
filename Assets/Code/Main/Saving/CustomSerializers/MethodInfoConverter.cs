using System;
using System.Reflection;
using Awaken.TG.MVC.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    [UnityEngine.Scripting.Preserve]
    public class MethodInfoConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            MethodInfo method = (MethodInfo) value;
            Type declaringType = method.DeclaringType;
            JObject obj = new JObject();
            obj["method"] = method.Name;
            obj["type"] = declaringType.AssemblyQualifiedName;
            serializer.Serialize(writer, obj);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            JObject obj = JObject.Load(reader);
            string methodName = (string)obj["method"];
            string typeName = (string) obj["type"];

            Type type = Type.GetType(typeName);
            if (type == null) {
                return null;
            }
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return methodInfo;
        }

        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(MethodInfo), objectType);
        }
    }
}