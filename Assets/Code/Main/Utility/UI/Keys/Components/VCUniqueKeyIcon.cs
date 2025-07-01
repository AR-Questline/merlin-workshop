using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.UI.Keys.Components {
    [RequireComponent(typeof(KeyIcon))]
    public class VCUniqueKeyIcon : ViewComponent<IUniqueKeyProvider> {
        protected override void OnAttach() {
            var keyIcon = GetComponent<KeyIcon>();
            keyIcon.Setup(Target.UniqueKey, ParentView);
            if (Target.UniqueKey.IsHold) {
                Target.RegisterForHold(keyIcon);
            }
        }
    }

    public interface IUniqueKeyProvider : IModel {
        KeyIcon.Data UniqueKey { get; }

        void RegisterForHold(KeyIcon keyIcon) {
            Log.Important?.Error("Hold unique key was declared but registering was not implemented!", keyIcon);
        }
    }
}