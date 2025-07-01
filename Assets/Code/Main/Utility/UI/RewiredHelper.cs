using System;
using System.Linq;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Rewired;
using UnityEngine;

#if UNITY_PS5
using Rewired.Platforms.PS5;
#endif

namespace Awaken.TG.Main.Utility.UI {
    public static class RewiredHelper {
        public const float DeadZone = 0.1f;
        public const float NaviThreshold = 0.4f;
        public const float ContinuousNaviThreshold = 0.9f;
        
        const float DefaultMSVibrationLengthModifier = 1f;
        const float DefaultSonyVibrationLengthModifier = 0.75f;
        const float DefaultOtherVibrationLengthModifier = 0.85f;
        const float UiSonyVibrationLengthModifier = 0.33f;
        const float UiOtherVibrationLengthModifier = 0.5f;
        const float UiSonyVibrationStrengthModifier = 0.5f;
        const float UiOtherVibrationStrengthModifier = 0.66f;
        
        // === Active controller
        static Controller s_currentController;
        static ControllerType s_controllerType;
        static GamepadManufacturer s_gamepadManufacturer;
        static int s_lastFrameGamepadCheck;
        public static bool IsGamepad => ActiveController() == ControllerType.Joystick;
        public static bool IsSony => RetrieveGamepadManufacturer(s_currentController) == GamepadManufacturer.Sony;
        public static bool IsDualSense => IsGamepad && s_currentController.hardwareTypeGuid == ControllerKey.DualSenseGuid;
        public static bool IsDualShock4 => IsGamepad && s_currentController.hardwareTypeGuid == ControllerKey.DualShock4Guid;
        public static bool IsXboxOne => IsGamepad && s_currentController.hardwareTypeGuid == ControllerKey.XboxOneGuid;
        public static bool IsXboxSeries => IsGamepad && s_currentController.hardwareTypeGuid == ControllerKey.XboxSeriesGuid;
        public static bool IsXboxOneOrSeries => IsXboxOne || IsXboxSeries;
        public static bool IsCurrentControllerConnected => IsGamepad && s_currentController.isConnected;
        public static Player Player => null; //ReInput.players.GetPlayer(0);
        public static int DefaultKeyboardLayoutId => 0; //ReInput.mapping.GetKeyboardLayoutId("Default");
        public static int AzertyKeyboardLayoutId => 0; //ReInput.mapping.GetKeyboardLayoutId("azertyKeyboard");
        
        [UnityEngine.Scripting.Preserve]
        public static string KeyIdentifierFor(string actionName, Pole pole = Pole.Positive) {
            return "None";
            // return Player.controllers.maps.GetAllMaps(ControllerType.Keyboard)
            //     .SelectMany(m => m.GetElementMapsWithAction(actionName))
            //     .FirstOrDefault(am => am.axisContribution == pole)?.elementIdentifierName ?? "None";
        }

        // === Blocking keyboard input
        [UnityEngine.Scripting.Preserve]
        public static void BlockKeyboardInput() {
            // foreach (var map in Player.controllers.maps.GetAllMaps()) {
            //     map.enabled = false;
            // }
        }

        [UnityEngine.Scripting.Preserve]
        public static void EnableKeyboardInput() {
            // foreach (var map in Player.controllers.maps.GetAllMaps()) {
            //     if (map.controllerType == ControllerType.Keyboard) {
            //         bool isAzerty = KeyboardLayoutDetector.IsAzertyKeyboard();
            //         int currentLayoutId = isAzerty ? AzertyKeyboardLayoutId : DefaultKeyboardLayoutId;
            //         map.enabled = map.layoutId == currentLayoutId;
            //     } else {
            //         map.enabled = true;
            //     }
            // }
        }

        [UnityEngine.Scripting.Preserve]
        public static void VibrateLowFreqPercentageBased(VibrationStrength from, VibrationStrength to, float percentage, VibrationDuration duration) {
            float strength = Mathf.Lerp(GetVibrationStrength(from), GetVibrationStrength(to), percentage);
            bool reset = duration == VibrationDuration.Continuous;
            if (s_gamepadManufacturer == GamepadManufacturer.Sony) {
                Vibrate(1, strength, GetVibrationDuration(duration), reset);
            }  else {
                Vibrate(0, strength, GetVibrationDuration(duration), reset);
            }
        }

        /// <summary>
        /// Check if two input actions have the same key binding
        /// </summary>
        public static bool IsEqualElementMapKey(int actionA, string actionB) {
            return false;
            // var currentController = ActiveController();
            // var mapsHelper = Player.controllers.maps;
            // var elementMapA = mapsHelper.GetFirstElementMapWithAction(currentController, actionA, true);
            // var elementMapB = mapsHelper.GetFirstElementMapWithAction(currentController, actionB, true);
            // return elementMapA != null && elementMapB != null && elementMapA.elementIdentifierId == elementMapB.elementIdentifierId;
        }

        // === Vibration
        /// <summary>
        /// Subtle vibrations for UI
        /// </summary>
        public static void VibrateUIHover(VibrationStrength strength, VibrationDuration duration) {
            if (!IsGamepad || !World.Only<PadVibrations>().Enabled) return;
            
            float strengthValue = GetVibrationStrength(strength);
            float durationValue = GetVibrationDuration(duration);
            bool reset = duration == VibrationDuration.Continuous;

            switch (s_gamepadManufacturer) {
                case GamepadManufacturer.MS:
                    //Player.SetVibration(0, strengthValue, durationValue, reset);
                    break;
                case GamepadManufacturer.Sony:
                    if (TryVibratePS5Controller(1, strengthValue, durationValue, reset)) {
                        break;
                    }

                    //Player.SetVibration(1, strengthValue * UiSonyVibrationStrengthModifier, durationValue * UiSonyVibrationLengthModifier, reset);
                    break;
                default:
                    //Player.SetVibration(0, strengthValue * UiOtherVibrationStrengthModifier, durationValue * UiOtherVibrationLengthModifier, reset);
                    break;
            }
        }

        /// <summary>
        /// Low frequency of vibration
        /// </summary>
        public static void VibrateLowFreq(VibrationStrength strength, VibrationDuration duration) {
            if (s_gamepadManufacturer == GamepadManufacturer.Sony) {
                Vibrate(1, strength, duration);
            } else {
                Vibrate(0, strength, duration);
            }
        }

        /// <summary>
        /// High frequency of vibration
        /// </summary>
        
        public static void VibrateHighFreq(VibrationStrength strength, VibrationDuration duration) {
            if (s_gamepadManufacturer == GamepadManufacturer.Sony) {
                Vibrate(0, strength, duration);
            }else {
                Vibrate(1, strength, duration);
            }
        }
        
        public static void StopVibration() {
            //Player.StopVibration();
        }

        static void Vibrate(int motor, VibrationStrength strength, VibrationDuration duration) {
            if (!IsGamepad || !World.Only<PadVibrations>().Enabled) return;
            
            float strengthValue = GetVibrationStrength(strength);
            float durationValue = GetVibrationDuration(duration);

            bool reset = duration == VibrationDuration.Continuous;
            Vibrate(motor, strengthValue, durationValue, reset);
        }
        
        static void Vibrate(int motor, float strength, float duration, bool reset) {
            duration *= s_gamepadManufacturer switch {
                GamepadManufacturer.MS => DefaultMSVibrationLengthModifier,
                GamepadManufacturer.Sony => DefaultSonyVibrationLengthModifier,
                _ => DefaultOtherVibrationLengthModifier
            };

            if (TryVibratePS5Controller(motor, strength, duration, reset)) {
                return;
            }

            //Player.SetVibration(motor, strength, duration, reset);
        }
        
        public static void EDITOR_RuntimeReset() {
            s_currentController = null;
            s_lastFrameGamepadCheck = 0;
        }

        public static ControllerType ActiveController() {
            if (Time.frameCount == s_lastFrameGamepadCheck) {
                return s_controllerType;
            }

            // var controller = Player.controllers.GetLastActiveController();
            // if (controller != s_currentController && CurrentAndPreviousIsKeyboardOrMouse(controller) == false) {
            //     s_currentController = controller;
            //     s_gamepadManufacturer = RetrieveGamepadManufacturer(controller);
            //     World.Services?.TryGet<UIKeyMapping>()?.RefreshMapping();
            // }
            //s_controllerType = RetrieveControllerType(controller);
            
            s_lastFrameGamepadCheck = Time.frameCount;
            return s_controllerType;
        }
        
        static bool CurrentAndPreviousIsKeyboardOrMouse(Controller lastActiveController) {
            if (s_currentController == null || lastActiveController == null) return false;
            return ControlSchemes.Get(s_currentController.type) is ControlScheme.KeyboardAndMouse && ControlSchemes.Get(lastActiveController.type) is ControlScheme.KeyboardAndMouse;
        }

        static ControllerType RetrieveControllerType(Controller controller) {
            return ControllerType.Mouse;
            // if (!Player.controllers.hasMouse) {
            //     return ControllerType.Joystick;
            // }
            //
            // ControllerType type = controller?.type ?? ControllerType.Custom;
            // if (type == ControllerType.Joystick) {
            //     // Actually Rewired lies, when you use joystick and then switch to mouse, without touching the keyboard.
            //     // It would return Joystick in such situation, that's why we give it another check.
            //     double joystickActiveTime = RewiredHelper.Player.controllers.GetLastActiveController<Joystick>()?.GetLastTimeActive() ?? 0f;
            //     double mouseActiveTime = RewiredHelper.Player.controllers.Mouse?.GetLastTimeActive() ?? 0f;
            //
            //     if (joystickActiveTime > mouseActiveTime) {
            //         return ControllerType.Joystick;
            //     } else {
            //         return ControllerType.Mouse;
            //     }
            // } else {
            //     return type;
            // }
        }

        static GamepadManufacturer RetrieveGamepadManufacturer(Controller controller) {
            // if (controller == null || controller.type != ControllerType.Joystick && controller.type != ControllerType.Custom) {
            //     return GamepadManufacturer.Other;
            // }
            //
            // if (controller.name.Contains("XInput") || controller.name.Contains("Microsoft") || controller.name.Contains("XBox")) {
            //     return GamepadManufacturer.MS;
            // }
            //
            // if (controller.name.Contains("Sony") || controller.name.Contains("PS") ||
            //     controller.name.Contains("DualSense")) {
            //     return GamepadManufacturer.Sony;
            // } 
            
            return GamepadManufacturer.Other;
        }

        public static float GetVibrationStrength(VibrationStrength strength) {
            return strength switch {
                VibrationStrength.VeryLow => 0.05f,
                VibrationStrength.Low => 0.2f,
                VibrationStrength.Medium => 0.5f,
                VibrationStrength.Strong => 0.8f,
                VibrationStrength.VeryStrong => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(strength), strength, null)
            };
        }

        public static float GetVibrationDuration(VibrationDuration duration) {
            return duration switch {
                VibrationDuration.Continuous => 0.1f,
                VibrationDuration.VeryShort => 0.15f,
                VibrationDuration.Short => 0.25f,
                VibrationDuration.Medium => 0.5f,
                VibrationDuration.Long => 0.75f,
                VibrationDuration.VeryLong => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null)
            };
        }

        static bool TryVibratePS5Controller(int motor, float strength, float duration, bool reset) {
#if UNITY_PS5
            if (Player.controllers.GetLastActiveController().extension is PS5GamepadExtension dualSense) {
                var motorType = motor switch {
                    0 => PS5GamepadMotorType.StrongMotor,
                    1 => PS5GamepadMotorType.WeakMotor,
                    _ => PS5GamepadMotorType.LeftMotor,
                };

                dualSense.SetVibration(motorType, strength, duration, reset);
                return true;
            }
#endif

            return false;
        }
    }

    public enum VibrationStrength {
        VeryLow = 0,
        Low = 1,
        Medium = 2,
        Strong = 3,
        VeryStrong = 4,
    }

    public enum VibrationDuration {
        Continuous = 0,
        VeryShort = 1,
        Short = 2,
        Medium = 3,
        Long = 4,
        VeryLong = 5,
    }

    public enum GamepadManufacturer {
        MS = 0,
        Sony = 1,
        Other = 2
    }
}