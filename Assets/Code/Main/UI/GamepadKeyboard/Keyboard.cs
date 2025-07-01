using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Sources;
using Rewired;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.GamepadKeyboard {
    public partial class Keyboard : Element<Model>, IUIStateSource {
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.NewShortcutLayer;

        // === Fields & Properties
        public TMP_InputField InputField { get; }
        Transform ViewParent { get; }

        public new static class Events {
            public static readonly Event<Keyboard, bool> InputAccepted = new(nameof(InputAccepted));
            public static readonly Event<Keyboard, bool> InputCanceled = new(nameof(InputCanceled));
        }
        
        // === Constructor
        public Keyboard(Transform viewParent, TMP_InputField inputField) {
            ViewParent = viewParent;
            InputField = inputField;
        }
        
        public Keyboard(Transform viewParent, ARInputField inputField) {
            ViewParent = viewParent;
            InputField = inputField.TMPInputField;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            World.SpawnView<VKeyboard>(this, true, forcedParent: ViewParent);
            World.Only<Focus>().ListenTo(Focus.Events.ControllerChanged, c => {
                if (c != ControllerType.Joystick) {
                    Discard();
                }
            }, this);
        }
    }
}
