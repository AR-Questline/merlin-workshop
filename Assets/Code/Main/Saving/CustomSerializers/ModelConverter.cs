using System;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Saving.CustomSerializers {
    public class ModelConverter : JsonConverter {
        public static Func<string, Model> idInterpreter;
        public static Domain currentSerializingDomain;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Write(writer, value as IModel);
        }

        public static void Write(JsonWriter writer, IModel model) {
            if (model.IsNotSaved) {
                Log.Critical?.Error("Trying to save a model that is not saved: " + LogUtils.GetDebugName(model) + " (" + writer.Path + ")");
            }
#if UNITY_EDITOR || AR_DEBUG
            if (model.CurrentDomain != currentSerializingDomain) {
                if (NotIgnoredDebug(writer)) {
                    Log.Critical?.Error(
                        "Trying to save a model that is not in the current domain: " + LogUtils.GetDebugName(model) +
                        " (" + writer.Path + ")");
                }
            }
#endif
            writer.WriteValue(Convert(model));
        }

        static bool NotIgnoredDebug(JsonWriter writer) {
            return !writer.Path.EndsWith("_killer") 
                   && !writer.Path.Contains("CharacterAntagonism");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            string id = (string) reader.Value;
            if (string.IsNullOrEmpty(id)) {
                return null;
            } else {
                return idInterpreter?.Invoke(id);
            }
        }

        public override bool CanConvert(Type objectType) {
            return AttributesCache.GetIsAssignableFrom(typeof(IModel), objectType);
        }
        
        public static string Convert(IModel model) => model?.ID ?? string.Empty;
        public static T Convert<T>(string id) where T : Model => (T)idInterpreter?.Invoke(id);
    }
}
