using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;

namespace Awaken.TG.MVC.Serialization {
    public partial class SaveWriter {
        public void WriteModel<T>(T value) where T : class, IModel {
            if (value == null) {
                WriteByte(0);
                return;
            }
            if (value.IsNotSaved) {
                Log.Critical?.Error("Trying to save a model that is not saved: " + LogUtils.GetDebugName(value));
            }
            if (value.CurrentDomain != _context.domain) {
                Log.Critical?.Error("Trying to save a model that is not in the current domain: " + LogUtils.GetDebugName(value));
            }
            WriteAscii(value.ID);
        }

        public void WriteTemplate<T>(in T value) where T : class, ITemplate {
            WriteAscii(value?.GUID);
        }

        public void WriteRichEnum<T>(T value) where T : RichEnum {
            WriteAscii(value?.Serialize());
        }
    }
}