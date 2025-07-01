using System;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.UI.Components.PadShortcuts;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.GamepadKeyboard {
    public class VCKeyboardButton : ViewComponent, IShortcutAction {
        public Transform keyboardParent;
        public TMP_InputField inputField;
        public ARSelectable selectable;

        public bool Active => true;
#pragma warning disable CS0067
        public event Action OnActiveChange;
#pragma warning restore CS0067

        UIResult IShortcutAction.Invoke() {
            return ShowKeyboard() != null ? UIResult.Accept : UIResult.Ignore;
        }

        public Keyboard ShowKeyboard() {
            if (RewiredHelper.IsGamepad && !World.HasAny<Keyboard>()) {
                var keyboard = new Keyboard(keyboardParent, inputField);
                ParentView.GenericTarget.AddElement(keyboard);
                keyboard.ListenTo(Model.Events.AfterDiscarded, OnKeyboardHide, this);
                return keyboard;
            }
            return null;
        }

        void OnKeyboardHide() {
            World.Only<Focus>().Select(selectable);
        }
    }
}