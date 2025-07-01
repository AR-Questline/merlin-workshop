using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;

namespace Awaken.TG.MVC.Serialization {
    public partial class SaveReader {
        public void ReadModel<T>(out T value) where T : class, IModel {
            value = null;
            ReadAscii(out var id);
            if (string.IsNullOrWhiteSpace(id)) {
                return;
            }

            if (!_context.deserializedModels.TryGetValue(id, out Model model)) {
                model = World.ByID(id);
            }

            if (model is T tModel) {
                value = tModel;
            } else if (model is null) {
                Log.Critical?.Error($"Model {id} doesn't exist, but loading is continued");
            } else {
                Log.Critical?.Error($"Model {id} is not of type {typeof(T)}, but loading is continued");
            }
        }

        public void ReadTemplate<T>(out T value) where T : class, ITemplate {
            ReadAscii(out var guid);
            if (string.IsNullOrWhiteSpace(guid)) {
                value = null;
            } else {
                value = TemplatesUtil.Load<T>(guid);
            }
        }

        public void ReadRichEnum<T>(out T value) where T : RichEnum {
            ReadAscii(out var enumString);
            if (string.IsNullOrWhiteSpace(enumString)) {
                value = null;
                return;
            }
            value = RichEnum.Deserialize<T>(enumString);
        }
    }
}