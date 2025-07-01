using System;

namespace Awaken.TG.Main.UI.Popup {
    public struct ButtonInfo {
        [UnityEngine.Scripting.Preserve] public Action callback;
        [UnityEngine.Scripting.Preserve] public string text;
        [UnityEngine.Scripting.Preserve] public bool visible;

        [UnityEngine.Scripting.Preserve]
        public static ButtonInfo Invisible => new ButtonInfo("", null, false);

        public ButtonInfo(string text, Action callback, bool visible = true) {
            this.callback = callback;
            this.text = text;
            this.visible = visible;
        }
    }
}