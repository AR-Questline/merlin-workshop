using System;
using Rewired;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Keys {
    /// <summary>
    /// based on: <br/>
    /// https://guavaman.com/projects/rewired/docs/HowTos.html#display-glyph-for-action <br/> <br/>
    /// http://guavaman.com/rewired/files/docs/RewiredControllerElementIdentifiersCSV.zip <br/>
    /// http://guavaman.com/rewired/files/docs/RewiredControllerTemplateElementIdentifiersCSV.zip <br/>
    /// http://guavaman.com/rewired/files/docs/RewiredMouseElementIdentifiersCSV.zip <br/>
    /// https://guavaman.com/rewired/files/docs/RewiredKeyboardElementIdentifiersCSV.zip <br/>
    /// note: order matters! its based on ElementIdentifierID
    /// </summary>
    public static class ControllerKey {
        // bypass KeyBindings, use for visual only key icons
        public enum CustomVisualOnlyKey : byte {
            [UnityEngine.Scripting.Preserve] None,
            MoveXY1, MoveXY2, MoveXY3,
        }

        public enum Xbox : byte {
            LeftStickX, LeftStickY,
            RightStickX, RightStickY,
            LeftTrigger, RightTrigger,
            A, B, X, Y,
            LeftShoulder, RightShoulder,
            View, Menu,
            LeftStickButton, RightStickButton,
            DPadUp, DPadRight, DPadDown, DPadLeft,
            LeftStick, RightStick,
            Guide, 
            //not handled by rewired
            LeftStickXY, RightStickXY, DPadXY,
        }
        
        public const int XboxCount = 26;
        public static readonly Guid XboxSeriesGuid = Guid.Parse("d74a350e-fe8b-4e9e-bbcd-efff16d34115");
        public static readonly Guid XboxOneGuid = Guid.Parse("19002688-7406-4f4a-8340-8d25335406c8");
        public static readonly Guid Xbox360Guid = Guid.Parse("d74a350e-fe8b-4e9e-bbcd-efff16d34115");
        
        public static Xbox GetXbox(ActionElementMap element) => (Xbox)element.elementIdentifierId;
        public static Xbox GetXbox(int elementIdentifierId) => (Xbox)elementIdentifierId;
        
        public enum DualShock2 : byte {
            LeftStickX, LeftStickY,
            RightStickX, RightStickY,
            Cross, Circle, Square, Triangle,
            L1, L2, R1, R2,
            Select, Start,
            LeftStickButton, RightStickButton,
            DPadUp, DPadRight, DPadDown, DPadLeft,
            LeftStick, RightStick,
            //not handled by rewired
            LeftStickXY, RightStickXY, DPadXY,
        }
        [UnityEngine.Scripting.Preserve] public const int DualShock2Count = 25;
        public static readonly Guid DualShock2Guid = Guid.Parse("c3ad3cad-c7cf-4ca8-8c2e-e3df8d9960bb");
        public static DualShock2 GetDualShock2(ActionElementMap element) => (DualShock2)element.elementIdentifierId;

        public enum DualShock3 : byte {
            LeftStickX, LeftStickY,
            RightStickX, RightStickY,
            Cross, Circle, Square, Triangle,
            L1, L2, R1, R2,
            Select, Start, PSButton,
            LeftStickButton, RightStickButton,
            DPadUp, DPadRight, DPadDown, DPadLeft,
            LeftStick, RightStick,
            //not handled by rewired
            LeftStickXY, RightStickXY, DPadXY,
        }
        [UnityEngine.Scripting.Preserve] public const int DualShock3Count = 26;
        public static readonly Guid DualShock3Guid = Guid.Parse("71dfe6c8-9e81-428f-a58e-c7e664b7fbed");
        public static DualShock3 GetDualShock3(ActionElementMap element) => (DualShock3)element.elementIdentifierId;
        
        
        public enum DualShock4 : byte {
            LeftStickX, LeftStickY,
            RightStickX, RightStickY,
            L2, R2,
            Cross, Circle, Square, Triangle,
            L1, R1,
            Share, Options, PSButton, TouchpadButton,
            LeftStickButton, RightStickButton,
            DPadUp, DPadRight, DPadDown, DPadLeft,
            LeftStick, RightStick,
            //not handled by rewired
            LeftStickXY, RightStickXY, DPadXY,
        }
        [UnityEngine.Scripting.Preserve] public const int DualShock4Count = 27;
        public static readonly Guid DualShock4Guid = Guid.Parse("cd9718bf-a87a-44bc-8716-60a0def28a9f");
        public static DualShock4 GetDualShock4(ActionElementMap element) => (DualShock4)element.elementIdentifierId;


        public enum DualSense : byte {
            LeftStickX, LeftStickY,
            RightStickX, RightStickY,
            L2, R2,
            Cross, Circle, Square, Triangle,
            L1, R1,
            Create, Options, PSButton, TouchpadButton,
            LeftStickButton, RightStickButton,
            DPadUp, DPadRight, DPadDown, DPadLeft,
            LeftStick, RightStick,
            Mute, 
            // not handled by rewired
            LeftStickXY, RightStickXY, DPadXY,
        }
        public const int DualSenseCount = 28;
        public static readonly Guid DualSenseGuid = Guid.Parse("5286706d-19b4-4a45-b635-207ce78d8394");
        public static DualSense GetDualSense(ActionElementMap element) => (DualSense)element.elementIdentifierId;
        public static DualSense GetDualSense(int elementIdentifierId) => (DualSense)elementIdentifierId;
        
        public enum GamepadTemplate : byte {
            LeftStickX, LeftStickY,
            RightStickX, RightStickY,
            ActionBottomRow1, ActionBottomRow2, ActionBottomRow3,
            ActionTopRow1, ActionTopRow2, ActionTopRow3,
            LeftShoulder1, LeftShoulder2,
            RightShoulder1, RightShoulder2,
            Center1, Center2, Center3,
            LeftStickButton, RightStickButton,
            DPadUp, DPadRight, DPadDown, DPadLeft,
            LeftStick, RightStick,
            DPad, 
            // not handled by rewired
            LeftStickXY, RightStickXY, DPadXY,
        }
        public const int GamepadCount = 29;
        public static readonly Guid GamepadTemplateGuid = Guid.Parse("83b427e4-086f-47f3-bb06-be266abd1ca5");
        //public static GamepadTemplate GetGamepadTemplate(IControllerTemplateElement element) => (GamepadTemplate)element.id;

        
        public enum Mouse : byte {
            [UnityEngine.Scripting.Preserve] MouseHorizontal, MouseVertical,
            [UnityEngine.Scripting.Preserve] MouseWheel,
            [UnityEngine.Scripting.Preserve] LeftMouseButton, RightMouseButton,
            [UnityEngine.Scripting.Preserve] MouseButton3, MouseButton4, MouseButton5, MouseButton6, MouseButton7,
            [UnityEngine.Scripting.Preserve] MouseWheelHorizontal,
            //not handled by rewired
            [UnityEngine.Scripting.Preserve] MouseHorizontalVertical,
        }
        public const int MouseCount = 12;
        public static Mouse GetMouse(ActionElementMap element) => (Mouse)element.elementIdentifierId;
        public static Mouse GetMouse(int elementIdentifierId) => (Mouse)elementIdentifierId;

        [UnityEngine.Scripting.Preserve]
        public enum Keyboard : byte {
            None,
            A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, 
            Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
            Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
            KeypadDot, KeypadSlash, KeypadStart, KeypadMinus, KeypadPlus, KeypadEnter, KeypadEquals,
            Space, Backspace, Tab,
            Clear, Return, Pause, ESC,
            ExclamationMark, Quote, Hash, Dollar, Ampersand, SingleQuote,
            BracketLeft, BracketRight,
            Star, Plus, Comma, Minus, Dot, Slash, Colon, SemiColon, 
            LessThen, Equals, MoreThen, QuestionMark, AtSign,
            SquareBracketLeft, BackSlash,
            SquareBracketRight, Circumflex,
            Underscore, BackQuote, Delete,
            ArrowUp, ArrowDown, ArrowRight, ArrowLeft,
            Insert, Home, End,
            PageUp, PageDown,
            F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15,
            Numlock, CapsLock, ScrollLock,
            RightShift, LeftShift,
            RightControl, LeftControl,
            RightAlt, LeftAlt,
            RightCommand, LeftCommand,
            LeftWindows, RightWindows,
            AltGr, Help, Print, SysReq, Break, Menu,
        }
        public const int KeyboardCount = 132;
        public static Keyboard GetKeyboard(ActionElementMap element) => (Keyboard)element.elementIdentifierId;
        public static Keyboard GetKeyboard(KeyCode keyCode) {
            return keyCode switch {
                KeyCode.A => Keyboard.A,
                KeyCode.B => Keyboard.B,
                KeyCode.C => Keyboard.C,
                KeyCode.D => Keyboard.D,
                KeyCode.E => Keyboard.E,
                KeyCode.F => Keyboard.F,
                KeyCode.G => Keyboard.G,
                KeyCode.H => Keyboard.H,
                KeyCode.I => Keyboard.I,
                KeyCode.J => Keyboard.J,
                KeyCode.K => Keyboard.K,
                KeyCode.L => Keyboard.L,
                KeyCode.M => Keyboard.M,
                KeyCode.N => Keyboard.N,
                KeyCode.O => Keyboard.O,
                KeyCode.P => Keyboard.P,
                KeyCode.Q => Keyboard.Q,
                KeyCode.R => Keyboard.R,
                KeyCode.S => Keyboard.S,
                KeyCode.T => Keyboard.T,
                KeyCode.U => Keyboard.U,
                KeyCode.V => Keyboard.V,
                KeyCode.W => Keyboard.W,
                KeyCode.X => Keyboard.X,
                KeyCode.Y => Keyboard.Y,
                KeyCode.Z => Keyboard.Z,
                KeyCode.Alpha0 => Keyboard.Num0,
                KeyCode.Alpha1 => Keyboard.Num1,
                KeyCode.Alpha2 => Keyboard.Num2,
                KeyCode.Alpha3 => Keyboard.Num3,
                KeyCode.Alpha4 => Keyboard.Num4,
                KeyCode.Alpha5 => Keyboard.Num5,
                KeyCode.Alpha6 => Keyboard.Num6,
                KeyCode.Alpha7 => Keyboard.Num7,
                KeyCode.Alpha8 => Keyboard.Num8,
                KeyCode.Alpha9 => Keyboard.Num9,
                KeyCode.Keypad0 => Keyboard.Keypad0,
                KeyCode.Keypad1 => Keyboard.Keypad1,
                KeyCode.Keypad2 => Keyboard.Keypad2,
                KeyCode.Keypad3 => Keyboard.Keypad3,
                KeyCode.Keypad4 => Keyboard.Keypad4,
                KeyCode.Keypad5 => Keyboard.Keypad5,
                KeyCode.Keypad6 => Keyboard.Keypad6,
                KeyCode.Keypad7 => Keyboard.Keypad7,
                KeyCode.Keypad8 => Keyboard.Keypad8,
                KeyCode.Keypad9 => Keyboard.Keypad9,
                KeyCode.Slash => Keyboard.KeypadSlash,
                KeyCode.KeypadMinus => Keyboard.KeypadMinus,
                KeyCode.KeypadPlus => Keyboard.KeypadPlus,
                KeyCode.KeypadEnter => Keyboard.KeypadEnter,
                KeyCode.KeypadEquals => Keyboard.KeypadEquals,
                KeyCode.Space => Keyboard.Space,
                KeyCode.Backspace => Keyboard.Backspace,
                KeyCode.Tab => Keyboard.Tab,
                KeyCode.Clear => Keyboard.Clear,
                KeyCode.Return => Keyboard.Return,
                KeyCode.Pause => Keyboard.Pause,
                KeyCode.Escape => Keyboard.ESC,
                KeyCode.Exclaim => Keyboard.ExclamationMark,
                KeyCode.DoubleQuote => Keyboard.Quote,
                KeyCode.Hash => Keyboard.Hash,
                KeyCode.Dollar => Keyboard.Dollar,
                KeyCode.Ampersand => Keyboard.Ampersand,
                KeyCode.Quote => Keyboard.SingleQuote,
                KeyCode.LeftBracket => Keyboard.BracketLeft,
                KeyCode.RightBracket => Keyboard.BracketRight,
                KeyCode.Plus => Keyboard.Plus,
                KeyCode.Comma => Keyboard.Comma,
                KeyCode.Minus => Keyboard.Minus,
                KeyCode.Colon => Keyboard.Colon,
                KeyCode.Semicolon => Keyboard.SemiColon,
                KeyCode.Less => Keyboard.LessThen,
                KeyCode.Equals => Keyboard.Equals,
                KeyCode.Greater => Keyboard.MoreThen,
                KeyCode.Question => Keyboard.QuestionMark,
                KeyCode.At => Keyboard.AtSign,
                KeyCode.LeftCurlyBracket => Keyboard.SquareBracketLeft,
                KeyCode.Backslash => Keyboard.BackSlash,
                KeyCode.RightCurlyBracket => Keyboard.SquareBracketRight,
                KeyCode.Underscore => Keyboard.Underscore,
                KeyCode.BackQuote => Keyboard.BackQuote,
                KeyCode.Delete => Keyboard.Delete,
                KeyCode.UpArrow => Keyboard.ArrowUp,
                KeyCode.DownArrow => Keyboard.ArrowDown,
                KeyCode.RightArrow => Keyboard.ArrowRight,
                KeyCode.LeftArrow => Keyboard.ArrowLeft,
                KeyCode.Insert => Keyboard.Insert,
                KeyCode.Home => Keyboard.Home,
                KeyCode.End => Keyboard.End,
                KeyCode.PageUp => Keyboard.PageUp,
                KeyCode.PageDown => Keyboard.PageDown,
                KeyCode.F1 => Keyboard.F1,
                KeyCode.F2 => Keyboard.F2,
                KeyCode.F3 => Keyboard.F3,
                KeyCode.F4 => Keyboard.F4,
                KeyCode.F5 => Keyboard.F5,
                KeyCode.F6 => Keyboard.F6,
                KeyCode.F7 => Keyboard.F7,
                KeyCode.F8 => Keyboard.F8,
                KeyCode.F9 => Keyboard.F9,
                KeyCode.F10 => Keyboard.F10,
                KeyCode.F11 => Keyboard.F11,
                KeyCode.F12 => Keyboard.F12,
                KeyCode.F13 => Keyboard.F13,
                KeyCode.F14 => Keyboard.F14,
                KeyCode.F15 => Keyboard.F15,
                KeyCode.Numlock => Keyboard.Numlock,
                KeyCode.CapsLock => Keyboard.CapsLock,
                KeyCode.ScrollLock => Keyboard.ScrollLock,
                KeyCode.RightShift => Keyboard.RightShift,
                KeyCode.LeftShift => Keyboard.LeftShift,
                KeyCode.RightControl => Keyboard.RightControl,
                KeyCode.LeftControl => Keyboard.LeftControl,
                KeyCode.RightAlt => Keyboard.RightAlt,
                KeyCode.LeftAlt => Keyboard.LeftAlt,
                KeyCode.RightCommand => Keyboard.RightCommand,
                KeyCode.LeftCommand => Keyboard.LeftCommand,
                KeyCode.LeftWindows => Keyboard.LeftWindows,
                KeyCode.RightWindows => Keyboard.RightWindows,
                KeyCode.AltGr => Keyboard.AltGr,
                KeyCode.Help => Keyboard.Help,
                KeyCode.Print => Keyboard.Print,
                KeyCode.SysReq => Keyboard.SysReq,
                KeyCode.Break => Keyboard.Break,
                KeyCode.Menu => Keyboard.Menu,
                _ => Keyboard.None,
            };
        }
    }
}