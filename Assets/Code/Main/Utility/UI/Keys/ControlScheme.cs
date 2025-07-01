using System;
using Awaken.Utility;
using Rewired;

namespace Awaken.TG.Main.Utility.UI.Keys {
    public enum ControlScheme : byte {
        Gamepad = 0,
        KeyboardAndMouse = 1,
    }

    [Flags]
    public enum ControlSchemeFlag : byte {
        [UnityEngine.Scripting.Preserve] None = 0,
        Gamepad = 1 << ControlScheme.Gamepad,
        KeyboardAndMouse = 1 << ControlScheme.KeyboardAndMouse,
        
        All = Gamepad | KeyboardAndMouse,
    }
    
    public static class ControlSchemes {
        public const int Count = 2;

        public static ControlScheme Current() {
            return RewiredHelper.IsGamepad || PlatformUtils.IsSteamDeck ? ControlScheme.Gamepad : ControlScheme.KeyboardAndMouse;
        }

        public static ControlSchemeFlag CurrentAsFlag() {
            int schemeFlagInt = 1 << (int) Current();
            return (ControlSchemeFlag) schemeFlagInt;
        }

        public static ControlScheme Get(ControllerType controllerType) {
            return controllerType switch {
                ControllerType.Keyboard => ControlScheme.KeyboardAndMouse,
                ControllerType.Mouse => ControlScheme.KeyboardAndMouse,
                ControllerType.Joystick => ControlScheme.Gamepad,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static ControllerType[] Controllers(this ControlScheme scheme) {
            return scheme switch {
                ControlScheme.Gamepad => GamepadControllers,
                ControlScheme.KeyboardAndMouse => KeyboardAndMouseControllers,
                _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
            };
        }

        static readonly ControllerType[] KeyboardAndMouseControllers = { ControllerType.Keyboard, ControllerType.Mouse };
        static readonly ControllerType[] GamepadControllers = { ControllerType.Joystick };
    }
}