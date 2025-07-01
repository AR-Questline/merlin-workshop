using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Simple view component that changes the padding of a layout group based on the current controller type.
    /// Useful for adjust position of tabs element without gamepad/keyboard shortcuts.
    /// </summary>
    public class VCShortcutLayoutPadding : ViewComponent {
        [SerializeField] LayoutGroup layoutGroup;
        [SerializeField] RectOffset paddingGamepad;
        [SerializeField] RectOffset paddingKeyboard;
        
        protected override void OnAttach() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, Refresh);
            Refresh(RewiredHelper.IsGamepad ? ControllerType.Joystick : ControllerType.Keyboard);
        }
        
        void Refresh(ControllerType controllerType) {
            layoutGroup.padding = controllerType switch {
                ControllerType.Joystick => paddingGamepad,
                ControllerType.Keyboard => paddingKeyboard,
                _ => layoutGroup.padding
            };
        }
        

    }
}
