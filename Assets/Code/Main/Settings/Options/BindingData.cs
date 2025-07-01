using Awaken.Utility;
using System;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Utility.Attributes;
using Rewired;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options {
    public partial struct BindingData {
        public ushort TypeForSerialization => SavedTypes.BindingData;

        [Saved] public ControllerType controller;
        [Saved] public int elementIdentifierId;
        [Saved] public KeyCode keyCode;
        [Saved] public ControllerElementType elementType;
        [Saved] public Pole axisContribution;

        public readonly ControllerType Controller => controller;
        
        public BindingData(ControllerType controller, int elementIdentifierID, KeyCode keyCode, ControllerElementType elementType = ControllerElementType.Button, Pole axisContribution = Pole.Positive) {
            this.controller = controller;
            this.elementIdentifierId = elementIdentifierID;
            this.keyCode = keyCode;
            this.elementType = elementType;
            this.axisContribution = axisContribution;
        }

        [UnityEngine.Scripting.Preserve]
        public readonly bool TryGetForJoystick(out int elementIdentifierId) {
            elementIdentifierId = this.elementIdentifierId;
            return Controller == ControllerType.Joystick;
        }
        
        [UnityEngine.Scripting.Preserve]
        public readonly bool TryGetForDualSense(ActionElementMap map, out ControllerKey.DualSense key) {
            key = ControllerKey.GetDualSense(map);
            return Controller == ControllerType.Joystick;
        }
        
        [UnityEngine.Scripting.Preserve]
        public readonly bool TryGetForXbox(ActionElementMap map, out ControllerKey.Xbox key) {
            key = ControllerKey.GetXbox(map);
            return Controller == ControllerType.Joystick;
        }
        
        public readonly bool TryGetForKeyboard(out KeyCode keyCode) {
            keyCode = this.keyCode;
            return Controller == ControllerType.Keyboard;
        }
        public readonly bool TryGetForKeyboard(out ControllerKey.Keyboard key) {
            key = ControllerKey.GetKeyboard(keyCode);
            return Controller == ControllerType.Keyboard;
        }

        public readonly bool TryGetForMouse(out int elementIdentifierId) {
            elementIdentifierId = this.elementIdentifierId;
            return Controller == ControllerType.Mouse;
        }
        
        public readonly bool TryGetForMouse(out ControllerKey.Mouse key) {
            key = ControllerKey.GetMouse(elementIdentifierId);
            return Controller == ControllerType.Mouse;
        }

        public static bool operator ==(in BindingData lhs, in BindingData rhs) {
            return (lhs.Controller, rhs.Controller) switch {
                (ControllerType.Keyboard, ControllerType.Keyboard) => lhs.keyCode == rhs.keyCode,
                (ControllerType.Mouse, ControllerType.Mouse) => lhs.elementIdentifierId == rhs.elementIdentifierId,
                (ControllerType.Joystick, ControllerType.Joystick) => lhs.elementIdentifierId == rhs.elementIdentifierId,
                _ => false,
            };
        }
        public static bool operator !=(in BindingData lhs, in BindingData rhs) => !(lhs == rhs);
        
        public readonly bool Equals(BindingData other) => this == other;
        public readonly override bool Equals(object obj) => obj is BindingData other && Equals(other);

        public readonly override int GetHashCode() {
            return HashCode.Combine(
                (int)Controller,
                Controller == ControllerType.Keyboard ? (int)keyCode : elementIdentifierId
            );
        }
    }
}