using System;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Animations;
using Rewired;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VNewKeyBinding")]
    public class VNewKeyBinding : View<Model>, IUIAware, IAutoFocusBase {
        AlwaysPresentHandlers _handler;
        // === Events
        public static class Events {
            public static readonly Event<IModel, KeyPressedData> KeyPressed = new(nameof(KeyPressed));
            public static readonly Event<IModel, KeyCode> NewBindingCanceled = new(nameof(NewBindingCanceled));
        }

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            _handler = new AlwaysPresentHandlers(UIContext.All, this);
            World.Only<GameUI>().AddElement(_handler);
            World.EventSystem.ListenTo(EventSelector.AnySource, ISettingHolder.Events.KeyProcessed, this, Discard);
        }

        void Update() {
            // var lastActiveController = RewiredHelper.Player.controllers.GetLastActiveController();
            // if (lastActiveController is ControllerWithAxes controller && RewiredHelper.IsGamepad && !UIStateStack.Instance.State.IsMapInteractive) {
            //     foreach (var keyDown in controller.PollForAllElementsDown()) {
            //         HandleGamepadButtonPressed(keyDown.elementIdentifierId);
            //     }
            // }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIEKeyDown keyDown && !RewiredHelper.IsGamepad) {
                if (keyDown.Key == KeyCode.Escape) {
                    Target.Trigger(Events.NewBindingCanceled, keyDown.Key);
                } else {
                    Target.Trigger(Events.KeyPressed, new KeyPressedData((int)keyDown.Key, ControllerType.Keyboard));
                }
                return UIResult.Accept;
            }
            
            if (evt is UIEMouseDown mouseDown && !RewiredHelper.IsGamepad) {
                Target.Trigger(Events.KeyPressed, new KeyPressedData(mouseDown.Button, ControllerType.Mouse));
                return UIResult.Accept;
            }
            
            return UIResult.Prevent;
        }

        void HandleGamepadButtonPressed(int id) {
            // Guid guid = RewiredHelper.Player.controllers.GetLastActiveController().hardwareTypeGuid;
            //
            // if (guid == ControllerKey.DualSenseGuid || guid == ControllerKey.DualShock4Guid) {
            //     Target.Trigger(Events.KeyPressed, new KeyPressedData(id, ControllerType.Joystick));
            // } else {
            //     Target.Trigger(Events.KeyPressed, new KeyPressedData(id, ControllerType.Joystick));
            // } 
        }

        protected override IBackgroundTask OnDiscard() {
            _handler?.Discard();
            return base.OnDiscard();
        }
    }

    public struct KeyPressedData {
        public int id;
        public ControllerType controllerType;

        public KeyPressedData(int id, ControllerType controllerType) {
            this.id = id;
            this.controllerType = controllerType;
        }
    }
}