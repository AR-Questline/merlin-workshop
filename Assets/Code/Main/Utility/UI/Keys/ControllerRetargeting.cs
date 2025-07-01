using System;

namespace Awaken.TG.Main.Utility.UI.Keys {
    public static class ControllerRetargeting {
        public static ControllerKey.DualSense ToDualSense(this ControllerKey.DualShock2 dualShock) {
            return dualShock switch {
                ControllerKey.DualShock2.LeftStickX => ControllerKey.DualSense.LeftStickX,
                ControllerKey.DualShock2.LeftStickY => ControllerKey.DualSense.LeftStickY,
                ControllerKey.DualShock2.RightStickX => ControllerKey.DualSense.RightStickX,
                ControllerKey.DualShock2.RightStickY => ControllerKey.DualSense.RightStickY,
                ControllerKey.DualShock2.Cross => ControllerKey.DualSense.Cross,
                ControllerKey.DualShock2.Circle => ControllerKey.DualSense.Circle,
                ControllerKey.DualShock2.Square => ControllerKey.DualSense.Square,
                ControllerKey.DualShock2.Triangle => ControllerKey.DualSense.Triangle,
                ControllerKey.DualShock2.L1 => ControllerKey.DualSense.L1,
                ControllerKey.DualShock2.L2 => ControllerKey.DualSense.L2,
                ControllerKey.DualShock2.R1 => ControllerKey.DualSense.R1,
                ControllerKey.DualShock2.R2 => ControllerKey.DualSense.R2,
                ControllerKey.DualShock2.Select => ControllerKey.DualSense.Create,
                ControllerKey.DualShock2.Start => ControllerKey.DualSense.Options,
                ControllerKey.DualShock2.LeftStickButton => ControllerKey.DualSense.LeftStickButton,
                ControllerKey.DualShock2.RightStickButton => ControllerKey.DualSense.RightStickButton,
                ControllerKey.DualShock2.DPadUp => ControllerKey.DualSense.DPadUp,
                ControllerKey.DualShock2.DPadRight => ControllerKey.DualSense.DPadRight,
                ControllerKey.DualShock2.DPadDown => ControllerKey.DualSense.DPadDown,
                ControllerKey.DualShock2.DPadLeft => ControllerKey.DualSense.DPadLeft,
                ControllerKey.DualShock2.LeftStick => ControllerKey.DualSense.LeftStick,
                ControllerKey.DualShock2.RightStick => ControllerKey.DualSense.RightStick,
                ControllerKey.DualShock2.LeftStickXY => ControllerKey.DualSense.LeftStickXY,
                ControllerKey.DualShock2.RightStickXY => ControllerKey.DualSense.RightStickXY,
                ControllerKey.DualShock2.DPadXY => ControllerKey.DualSense.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(dualShock), dualShock, null)
            };
        }

        public static ControllerKey.DualSense ToDualSense(this ControllerKey.DualShock3 dualShock) {
            return dualShock switch {
                ControllerKey.DualShock3.LeftStickX => ControllerKey.DualSense.LeftStickX,
                ControllerKey.DualShock3.LeftStickY => ControllerKey.DualSense.LeftStickY,
                ControllerKey.DualShock3.RightStickX => ControllerKey.DualSense.RightStickX,
                ControllerKey.DualShock3.RightStickY => ControllerKey.DualSense.RightStickY,
                ControllerKey.DualShock3.Cross => ControllerKey.DualSense.Cross,
                ControllerKey.DualShock3.Circle => ControllerKey.DualSense.Circle,
                ControllerKey.DualShock3.Square => ControllerKey.DualSense.Square,
                ControllerKey.DualShock3.Triangle => ControllerKey.DualSense.Triangle,
                ControllerKey.DualShock3.L1 => ControllerKey.DualSense.L1,
                ControllerKey.DualShock3.L2 => ControllerKey.DualSense.L2,
                ControllerKey.DualShock3.R1 => ControllerKey.DualSense.R1,
                ControllerKey.DualShock3.R2 => ControllerKey.DualSense.R2,
                ControllerKey.DualShock3.Select => ControllerKey.DualSense.Create,
                ControllerKey.DualShock3.Start => ControllerKey.DualSense.Options,
                ControllerKey.DualShock3.PSButton => ControllerKey.DualSense.PSButton,
                ControllerKey.DualShock3.LeftStickButton => ControllerKey.DualSense.LeftStickButton,
                ControllerKey.DualShock3.RightStickButton => ControllerKey.DualSense.RightStickButton,
                ControllerKey.DualShock3.DPadUp => ControllerKey.DualSense.DPadUp,
                ControllerKey.DualShock3.DPadRight => ControllerKey.DualSense.DPadRight,
                ControllerKey.DualShock3.DPadDown => ControllerKey.DualSense.DPadDown,
                ControllerKey.DualShock3.DPadLeft => ControllerKey.DualSense.DPadLeft,
                ControllerKey.DualShock3.LeftStick => ControllerKey.DualSense.LeftStick,
                ControllerKey.DualShock3.RightStick => ControllerKey.DualSense.RightStick,
                ControllerKey.DualShock3.LeftStickXY => ControllerKey.DualSense.LeftStickXY,
                ControllerKey.DualShock3.RightStickXY => ControllerKey.DualSense.RightStickXY,
                ControllerKey.DualShock3.DPadXY => ControllerKey.DualSense.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(dualShock), dualShock, null)
            };
        }

        public static ControllerKey.DualSense ToDualSense(this ControllerKey.DualShock4 dualShock) {
            return dualShock switch {
                ControllerKey.DualShock4.LeftStickX => ControllerKey.DualSense.LeftStickX,
                ControllerKey.DualShock4.LeftStickY => ControllerKey.DualSense.LeftStickY,
                ControllerKey.DualShock4.RightStickX => ControllerKey.DualSense.RightStickX,
                ControllerKey.DualShock4.RightStickY => ControllerKey.DualSense.RightStickY,
                ControllerKey.DualShock4.L2 => ControllerKey.DualSense.L2,
                ControllerKey.DualShock4.R2 => ControllerKey.DualSense.R2,
                ControllerKey.DualShock4.Cross => ControllerKey.DualSense.Cross,
                ControllerKey.DualShock4.Circle => ControllerKey.DualSense.Circle,
                ControllerKey.DualShock4.Square => ControllerKey.DualSense.Square,
                ControllerKey.DualShock4.Triangle => ControllerKey.DualSense.Triangle,
                ControllerKey.DualShock4.L1 => ControllerKey.DualSense.L1,
                ControllerKey.DualShock4.R1 => ControllerKey.DualSense.R1,
                ControllerKey.DualShock4.Share => ControllerKey.DualSense.Create,
                ControllerKey.DualShock4.Options => ControllerKey.DualSense.Options,
                ControllerKey.DualShock4.PSButton => ControllerKey.DualSense.PSButton,
                ControllerKey.DualShock4.TouchpadButton => ControllerKey.DualSense.TouchpadButton,
                ControllerKey.DualShock4.LeftStickButton => ControllerKey.DualSense.LeftStickButton,
                ControllerKey.DualShock4.RightStickButton => ControllerKey.DualSense.RightStickButton,
                ControllerKey.DualShock4.DPadUp => ControllerKey.DualSense.DPadUp,
                ControllerKey.DualShock4.DPadRight => ControllerKey.DualSense.DPadRight,
                ControllerKey.DualShock4.DPadDown => ControllerKey.DualSense.DPadDown,
                ControllerKey.DualShock4.DPadLeft => ControllerKey.DualSense.DPadLeft,
                ControllerKey.DualShock4.LeftStick => ControllerKey.DualSense.LeftStick,
                ControllerKey.DualShock4.RightStick => ControllerKey.DualSense.RightStick,
                ControllerKey.DualShock4.LeftStickXY => ControllerKey.DualSense.LeftStickXY,
                ControllerKey.DualShock4.RightStickXY => ControllerKey.DualSense.RightStickXY,
                ControllerKey.DualShock4.DPadXY => ControllerKey.DualSense.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(dualShock), dualShock, null)
            };
        }

        [UnityEngine.Scripting.Preserve]
        public static ControllerKey.GamepadTemplate ToGamepadTemplate(this ControllerKey.DualSense dualSense) {
            return dualSense switch {
                ControllerKey.DualSense.LeftStickX => ControllerKey.GamepadTemplate.LeftStickX,
                ControllerKey.DualSense.LeftStickY => ControllerKey.GamepadTemplate.LeftStickY,
                ControllerKey.DualSense.RightStickX => ControllerKey.GamepadTemplate.RightStickX,
                ControllerKey.DualSense.RightStickY => ControllerKey.GamepadTemplate.RightStickY,
                ControllerKey.DualSense.L2 => ControllerKey.GamepadTemplate.LeftShoulder2,
                ControllerKey.DualSense.R2 => ControllerKey.GamepadTemplate.RightShoulder2,
                ControllerKey.DualSense.Cross => ControllerKey.GamepadTemplate.ActionBottomRow1,
                ControllerKey.DualSense.Circle => ControllerKey.GamepadTemplate.ActionBottomRow2,
                ControllerKey.DualSense.Square => ControllerKey.GamepadTemplate.ActionTopRow1,
                ControllerKey.DualSense.Triangle => ControllerKey.GamepadTemplate.ActionTopRow2,
                ControllerKey.DualSense.L1 => ControllerKey.GamepadTemplate.LeftShoulder1,
                ControllerKey.DualSense.R1 => ControllerKey.GamepadTemplate.RightShoulder1,
                ControllerKey.DualSense.Create => ControllerKey.GamepadTemplate.Center1,
                ControllerKey.DualSense.Options => ControllerKey.GamepadTemplate.Center2,
                ControllerKey.DualSense.PSButton => ControllerKey.GamepadTemplate.Center3,
                ControllerKey.DualSense.TouchpadButton => ControllerKey.GamepadTemplate.Center3,
                ControllerKey.DualSense.LeftStickButton => ControllerKey.GamepadTemplate.LeftStickButton,
                ControllerKey.DualSense.RightStickButton => ControllerKey.GamepadTemplate.RightStickButton,
                ControllerKey.DualSense.DPadUp => ControllerKey.GamepadTemplate.DPadUp,
                ControllerKey.DualSense.DPadRight => ControllerKey.GamepadTemplate.DPadRight,
                ControllerKey.DualSense.DPadDown => ControllerKey.GamepadTemplate.DPadDown,
                ControllerKey.DualSense.DPadLeft => ControllerKey.GamepadTemplate.DPadLeft,
                ControllerKey.DualSense.LeftStick => ControllerKey.GamepadTemplate.LeftStick,
                ControllerKey.DualSense.RightStick => ControllerKey.GamepadTemplate.RightStick,
                ControllerKey.DualSense.Mute => ControllerKey.GamepadTemplate.Center3,
                ControllerKey.DualSense.LeftStickXY => ControllerKey.GamepadTemplate.LeftStickXY,
                ControllerKey.DualSense.RightStickXY => ControllerKey.GamepadTemplate.RightStickXY,
                ControllerKey.DualSense.DPadXY => ControllerKey.GamepadTemplate.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(dualSense), dualSense, null)
            };
        }

        [UnityEngine.Scripting.Preserve]
        public static ControllerKey.Xbox ToXbox(this ControllerKey.DualSense dualSense) {
            const ControllerKey.Xbox unknown = ControllerKey.Xbox.View;
            return dualSense switch {
                ControllerKey.DualSense.LeftStickX => ControllerKey.Xbox.LeftStickX,
                ControllerKey.DualSense.LeftStickY => ControllerKey.Xbox.LeftStickY,
                ControllerKey.DualSense.RightStickX => ControllerKey.Xbox.RightStickX,
                ControllerKey.DualSense.RightStickY => ControllerKey.Xbox.RightStickY,
                ControllerKey.DualSense.L2 => ControllerKey.Xbox.LeftTrigger,
                ControllerKey.DualSense.R2 => ControllerKey.Xbox.RightTrigger,
                ControllerKey.DualSense.Cross => ControllerKey.Xbox.A,
                ControllerKey.DualSense.Circle => ControllerKey.Xbox.B,
                ControllerKey.DualSense.Square => ControllerKey.Xbox.X,
                ControllerKey.DualSense.Triangle => ControllerKey.Xbox.Y,
                ControllerKey.DualSense.L1 => ControllerKey.Xbox.LeftShoulder,
                ControllerKey.DualSense.R1 => ControllerKey.Xbox.RightShoulder,
                ControllerKey.DualSense.Create => ControllerKey.Xbox.View,
                ControllerKey.DualSense.Options => ControllerKey.Xbox.Menu,
                ControllerKey.DualSense.PSButton => unknown,
                ControllerKey.DualSense.TouchpadButton => unknown,
                ControllerKey.DualSense.LeftStickButton => ControllerKey.Xbox.LeftStickButton,
                ControllerKey.DualSense.RightStickButton => ControllerKey.Xbox.RightStickButton,
                ControllerKey.DualSense.DPadUp => ControllerKey.Xbox.DPadUp,
                ControllerKey.DualSense.DPadRight => ControllerKey.Xbox.DPadRight,
                ControllerKey.DualSense.DPadDown => ControllerKey.Xbox.DPadDown,
                ControllerKey.DualSense.DPadLeft => ControllerKey.Xbox.DPadLeft,
                ControllerKey.DualSense.LeftStick => ControllerKey.Xbox.LeftStick,
                ControllerKey.DualSense.RightStick => ControllerKey.Xbox.RightStick,
                ControllerKey.DualSense.Mute => unknown,
                ControllerKey.DualSense.LeftStickXY => ControllerKey.Xbox.LeftStickXY,
                ControllerKey.DualSense.RightStickXY => ControllerKey.Xbox.RightStickXY,
                ControllerKey.DualSense.DPadXY => ControllerKey.Xbox.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(dualSense), dualSense, null)
            };
        }

        public static ControllerKey.Xbox ToXbox(this ControllerKey.GamepadTemplate template) {
            const ControllerKey.Xbox unknown = ControllerKey.Xbox.View;
            return template switch {
                ControllerKey.GamepadTemplate.LeftStickX => ControllerKey.Xbox.LeftStickX,
                ControllerKey.GamepadTemplate.LeftStickY => ControllerKey.Xbox.LeftStickY,
                ControllerKey.GamepadTemplate.RightStickX => ControllerKey.Xbox.RightStickX,
                ControllerKey.GamepadTemplate.RightStickY => ControllerKey.Xbox.RightStickY,
                ControllerKey.GamepadTemplate.ActionBottomRow1 => ControllerKey.Xbox.A,
                ControllerKey.GamepadTemplate.ActionBottomRow2 => ControllerKey.Xbox.B,
                ControllerKey.GamepadTemplate.ActionBottomRow3 => unknown,
                ControllerKey.GamepadTemplate.ActionTopRow1 => ControllerKey.Xbox.X,
                ControllerKey.GamepadTemplate.ActionTopRow2 => ControllerKey.Xbox.Y,
                ControllerKey.GamepadTemplate.ActionTopRow3 => unknown,
                ControllerKey.GamepadTemplate.LeftShoulder1 => ControllerKey.Xbox.LeftShoulder,
                ControllerKey.GamepadTemplate.LeftShoulder2 => ControllerKey.Xbox.LeftTrigger,
                ControllerKey.GamepadTemplate.RightShoulder1 => ControllerKey.Xbox.RightShoulder,
                ControllerKey.GamepadTemplate.RightShoulder2 => ControllerKey.Xbox.RightTrigger,
                ControllerKey.GamepadTemplate.Center1 => ControllerKey.Xbox.View,
                ControllerKey.GamepadTemplate.Center2 => ControllerKey.Xbox.Menu,
                ControllerKey.GamepadTemplate.Center3 => ControllerKey.Xbox.View,
                ControllerKey.GamepadTemplate.LeftStickButton => ControllerKey.Xbox.LeftStickButton,
                ControllerKey.GamepadTemplate.RightStickButton => ControllerKey.Xbox.RightStickButton,
                ControllerKey.GamepadTemplate.DPadUp => ControllerKey.Xbox.DPadUp,
                ControllerKey.GamepadTemplate.DPadRight => ControllerKey.Xbox.DPadRight,
                ControllerKey.GamepadTemplate.DPadDown => ControllerKey.Xbox.DPadDown,
                ControllerKey.GamepadTemplate.DPadLeft => ControllerKey.Xbox.DPadLeft,
                ControllerKey.GamepadTemplate.LeftStick => ControllerKey.Xbox.LeftStick,
                ControllerKey.GamepadTemplate.RightStick => ControllerKey.Xbox.RightStick,
                ControllerKey.GamepadTemplate.DPad => unknown,
                ControllerKey.GamepadTemplate.LeftStickXY => ControllerKey.Xbox.LeftStickXY,
                ControllerKey.GamepadTemplate.RightStickXY => ControllerKey.Xbox.RightStickXY,
                ControllerKey.GamepadTemplate.DPadXY => ControllerKey.Xbox.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(template), template, null)
            };
        }

        public static ControllerKey.Xbox ToXbox(this ControllerKey.CustomVisualOnlyKey customKey) {
            return customKey switch {
                ControllerKey.CustomVisualOnlyKey.MoveXY1 => ControllerKey.Xbox.LeftStickXY,
                ControllerKey.CustomVisualOnlyKey.MoveXY2 => ControllerKey.Xbox.RightStickXY,
                ControllerKey.CustomVisualOnlyKey.MoveXY3 => ControllerKey.Xbox.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(customKey), customKey, null)
            };
        }
        
        public static ControllerKey.DualSense ToDualSense(this ControllerKey.CustomVisualOnlyKey customKey) {
            return customKey switch {
                ControllerKey.CustomVisualOnlyKey.MoveXY1 => ControllerKey.DualSense.LeftStickXY,
                ControllerKey.CustomVisualOnlyKey.MoveXY2 => ControllerKey.DualSense.RightStickXY,
                ControllerKey.CustomVisualOnlyKey.MoveXY3 => ControllerKey.DualSense.DPadXY,
                _ => throw new ArgumentOutOfRangeException(nameof(customKey), customKey, null)
            };
        }
        
        public static ControllerKey.Mouse ToMouse(this ControllerKey.CustomVisualOnlyKey customKey) {
            return customKey switch {
                ControllerKey.CustomVisualOnlyKey.MoveXY1 => ControllerKey.Mouse.MouseHorizontalVertical,
                ControllerKey.CustomVisualOnlyKey.MoveXY2 => ControllerKey.Mouse.MouseHorizontalVertical,
                ControllerKey.CustomVisualOnlyKey.MoveXY3 => ControllerKey.Mouse.MouseHorizontalVertical,
                _ => throw new ArgumentOutOfRangeException(nameof(customKey), customKey, null)
            };
        }
    }
}