using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class GamepadEffects : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.GamepadEffects;

        const int RightDualSenseTriggerIndex = (int)ControllerKey.DualSense.R2;
        const int LeftDualSenseTriggerIndex = (int)ControllerKey.DualSense.L2;
        const int RightXboxOneTriggerIndex = (int)ControllerKey.Xbox.RightTrigger;
        const int LeftXboxOneTriggerIndex = (int)ControllerKey.Xbox.LeftTrigger;
        const string AttackActionID = "Attack";
        const string BlockActionID = "Block";
        
        KeyBindingOption _dualSenseAttackKeyBindingOption;
        KeyBindingOption _dualSenseBlockKeyBindingOption;
        KeyBindingOption _xboxAttackKeyBindingOption;
        KeyBindingOption _xboxBlockKeyBindingOption;
        
        bool CanUseDualSenseTriggers => UIStateStack.Instance.State.IsMapInteractive && ParentModel.IsWeaponEquipped;

        public new static class Events {
            public static readonly Event<Hero, TriggersVibrationData> TriggerVibrations = new(nameof(TriggerVibrations));
        }
        
        protected override void OnInitialize() {
            // UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, UpdateDualSenseTriggerEffects, this);
            // World.Only<Focus>().ListenTo(Focus.Events.ControllerChanged, OnControllerChanged, this);
            // ParentModel.ListenTo(CharacterHandBase.Events.WeaponVisibilityToggled, UpdateDualSenseTriggerEffects, this);
            // ParentModel.ListenTo(Stat.Events.StatChanged(AliveStatType.Health), UpdateDualSenseLightColor, this);
            // ParentModel.ListenTo(Events.TriggerVibrations, GamepadTriggerVibrations, this);
            // ParentModel.ListenTo(CharacterBow.Events.OnBowIdleEntered, UpdateDualSenseTriggerEffects, this);
        }

        void GamepadTriggerVibrations(TriggersVibrationData triggersVibrationData) {
            if (RewiredHelper.IsXboxOneOrSeries && World.Only<AdaptiveTriggers>().Enabled) {
                XboxTriggerVibrations(triggersVibrationData);
            }
        }

        void XboxTriggerVibrations(TriggersVibrationData triggersVibrationData) {
            // //On pc with Windows Gaming Input enabled both Xbox One and Xbox Series controllers are recognized as Xbox One Gamepad, with XboxOneGuid
            // //So even if player has both Xbox One and Xbox Series controllers connected during session, only one GameControls is created for both of them
            // if (_xboxAttackKeyBindingOption == null || _xboxBlockKeyBindingOption == null) {
            //     var settings = World.Only<SettingsMaster>();
            //     var controls = GetCorrectGameControls(c => c.controllerIdentifier == ControllerKey.XboxOneGuid || c.controllerIdentifier == ControllerKey.XboxSeriesGuid);
            //     
            //     if (controls == null) {
            //         settings.InitOptionsForConnectedController();
            //         controls = GetCorrectGameControls(c => c.controllerIdentifier == ControllerKey.XboxOneGuid || c.controllerIdentifier == ControllerKey.XboxSeriesGuid);
            //     }
            //     
            //     _xboxAttackKeyBindingOption = controls.KeyBindings.Single(setting => setting.ID == AttackActionID);
            //     _xboxBlockKeyBindingOption = controls.KeyBindings.Single(setting => setting.ID == BlockActionID);
            // }
            //
            // if (!_xboxAttackKeyBindingOption.CurrentBinding.TryGetForJoystick(out int attackIdentifierId) ||
            //     !_xboxBlockKeyBindingOption.CurrentBinding.TryGetForJoystick(out int blockIdentifierId)) {
            //     return;
            // }
            //
            // bool isRightHandAttackTrigger = attackIdentifierId is RightXboxOneTriggerIndex or LeftXboxOneTriggerIndex;
            // bool isLeftHandAttackTrigger = blockIdentifierId is RightXboxOneTriggerIndex or LeftXboxOneTriggerIndex;
            // bool isRightHandAffected = isRightHandAttackTrigger && triggersVibrationData.handsAffected is CastingHand.MainHand or CastingHand.BothHands;
            // bool isLeftHandAffected = isLeftHandAttackTrigger && triggersVibrationData.handsAffected is CastingHand.OffHand or CastingHand.BothHands;

            // var currentControllerExtension = ReInput.controllers.GetLastActiveController().extension;
            //
            // //xbox controllers are using different extensions depending on platform, that's why they need to be handled separately for each platform
            // if (currentControllerExtension is XboxOneGamepadExtension xboxExtension) {
            //     foreach (var data in triggersVibrationData.effects) {
            //         if (isRightHandAffected) {
            //             xboxExtension.SetVibration(GetXboxMotorId(attackIdentifierId), data.strength, data.duration);
            //         }
            //
            //         if (isLeftHandAffected) {
            //             xboxExtension.SetVibration(GetXboxMotorId(blockIdentifierId), data.strength, data.duration);
            //         }
            //     }
            // } 
            
#if PLATFORM_STANDALONE_WIN
            // if (currentControllerExtension is WindowsGamingInputControllerExtension { controller: Joystick joystick }) {
            //     foreach (var data in triggersVibrationData.effects) {
            //         if (isRightHandAffected) {
            //             joystick.SetVibration(GetXboxMotorId(attackIdentifierId), data.strength, data.duration);
            //         }
            //
            //         if (isLeftHandAffected) {
            //             joystick.SetVibration(GetXboxMotorId(blockIdentifierId), data.strength, data.duration);
            //         }
            //     }
            // }
#endif
        }

//         int GetXboxMotorId(int actionIdentifierId) {
//             return actionIdentifierId == RightXboxOneTriggerIndex ? (int)XboxOneGamepadMotorType.RightTriggerMotor : (int)XboxOneGamepadMotorType.LeftTriggerMotor;
//         }
//
//         void OnControllerChanged(ControllerType controllerType) {
//             UpdateDualSenseDelayed().Forget();
//             UpdateDualSenseLightColor();
//             SetPS5VibraionMode();
//         }
//
//         [Conditional("UNITY_PS5")]
//         void SetPS5VibraionMode() {
// #if UNITY_PS5
//             if (ReInput.controllers.GetLastActiveController().extension is PS5GamepadExtension dualSenseExtension) {
//                 dualSenseExtension.SetVibrationMode(PS5GamepadVibrationMode.Advanced);
//             }
// #endif
//         }
//
//         async UniTaskVoid UpdateDualSenseDelayed() {
//             if (await AsyncUtil.WaitUntil(ParentModel, () => CanUseDualSenseTriggers)) {
//                 UpdateDualSenseTriggerEffects();
//             }
//         }
//
//         void UpdateDualSenseTriggerEffects() {
//             if (!RewiredHelper.IsDualSense) return;
//             
//             if (!CanUseDualSenseTriggers) {
//                 RemoveDualSenseEffects();
//                 return;
//             }
//             
//             if (_dualSenseAttackKeyBindingOption == null || _dualSenseBlockKeyBindingOption == null) {
//                 var settings = World.Only<SettingsMaster>();
//                 var controls = GetCorrectGameControls(c => c.controllerIdentifier == ControllerKey.DualSenseGuid);
//                 
//                 if (controls == null) {
//                     settings.InitOptionsForConnectedController();
//                     controls = GetCorrectGameControls(c => c.controllerIdentifier == ControllerKey.DualSenseGuid);
//                 }
//                 
//                 _dualSenseAttackKeyBindingOption = controls.KeyBindings.Single(setting => setting.ID == AttackActionID);
//                 _dualSenseBlockKeyBindingOption = controls.KeyBindings.Single(setting => setting.ID == BlockActionID);
//             }
//
//             if (!_dualSenseAttackKeyBindingOption.CurrentBinding.TryGetForJoystick(out int attackIdentifierId) ||
//                 !_dualSenseBlockKeyBindingOption.CurrentBinding.TryGetForJoystick(out int blockIdentifierId)) {
//                 return;
//             }
//             
//             bool isRightHandAttackTrigger = attackIdentifierId is RightDualSenseTriggerIndex or LeftDualSenseTriggerIndex;
//             bool isLeftHandAttackTrigger = blockIdentifierId is RightDualSenseTriggerIndex or LeftDualSenseTriggerIndex;
//
//             if (isRightHandAttackTrigger) {
//                 DualSenseTriggerType rightHandTrigger = attackIdentifierId == RightDualSenseTriggerIndex ? DualSenseTriggerType.Right : DualSenseTriggerType.Left;
//
//                 switch (ParentModel.MainHandWeapon) {
//                     case CharacterMagic:
//                         foreach (var effectData in GameConstants.Get.magicDualSenseEffects) {
//                             SetDualSenseTriggerEffect(rightHandTrigger, effectData.GetEffect());
//                         }
//
//                         break;
//                     case CharacterBow:
//                         if (!ParentModel.HasArrows) {
//                             RemoveDualSenseEffects();
//                             return;
//                         }
//                         
//                         foreach (var effectData in GameConstants.Get.bowDualSenseEffects) {
//                             SetDualSenseTriggerEffect(rightHandTrigger, effectData.GetEffect());
//                         }
//
//                         break;
//                     case CharacterShield:
//                         foreach (var effectData in GameConstants.Get.melee1HDualSenseEffects) {
//                             SetDualSenseTriggerEffect(rightHandTrigger, effectData.GetEffect());
//                         }
//
//                         break;
//                     case HeroFist:
//                         foreach (var effectData in GameConstants.Get.melee1HDualSenseEffects) {
//                             SetDualSenseTriggerEffect(rightHandTrigger, effectData.GetEffect());
//                         }
//
//                         break;
//                     case CharacterWeapon weapon:
//                         if (weapon.Item.IsTwoHanded) {
//                             foreach (var effectData in GameConstants.Get.melee2HDualSenseEffects) {
//                                 SetDualSenseTriggerEffect(rightHandTrigger, effectData.GetEffect());
//                             }
//                         } else {
//                             foreach (var effectData in GameConstants.Get.melee1HDualSenseEffects) {
//                                 SetDualSenseTriggerEffect(rightHandTrigger, effectData.GetEffect());
//                             }
//                         }
//
//                         break;
//                 }
//             }
//
//             if (isLeftHandAttackTrigger) {
//                 bool canBlock = ParentModel.MainHandWeapon is not CharacterBow && ParentModel.OffHandWeapon is not CharacterMagic;
//                 DualSenseTriggerType leftHandTrigger = blockIdentifierId == LeftDualSenseTriggerIndex ? DualSenseTriggerType.Left : DualSenseTriggerType.Right;
//
//                 if (canBlock) {
//                     foreach (var effectData in GameConstants.Get.blockDualSenseEffects) {
//                         SetDualSenseTriggerEffect(leftHandTrigger, effectData.GetEffect());
//                     }
//                 } else if (ParentModel.OffHandWeapon is CharacterMagic) {
//                     foreach (var effectData in GameConstants.Get.magicDualSenseEffects) {
//                         SetDualSenseTriggerEffect(leftHandTrigger, effectData.GetEffect());
//                     }
//                 } else {
//                     DisableDualSenseTriggerEffects(DualSenseTriggerType.Left);
//                 }
//             }
//         }
//         
//         GameControls GetCorrectGameControls(Func <GameControls, bool> predicate){
//             var settings = World.Only<SettingsMaster>();
//             return settings.GamepadControlsSettings.SingleOrDefault(predicate);
//         }
//
//         void RemoveDualSenseEffects() {
//             DisableDualSenseTriggerEffects(DualSenseTriggerType.Right);
//             DisableDualSenseTriggerEffects(DualSenseTriggerType.Left);
//         }
//
//         void UpdateDualSenseLightColor() {
//             UpdateDualSenseLightColor(Hero.Current.HealthStat);
//         }
//
//         void UpdateDualSenseLightColor(Stat health) {
//             if (!RewiredHelper.IsDualSense && !RewiredHelper.IsDualShock4) return;
//
//             if (health is LimitedStat limitedStatHealth) {
//                 SetDualSenseDualShock4LightColor(limitedStatHealth.Percentage > 0.25 ? Color.green : Color.red);
//             }
//         }
//
//         static void DisableDualSenseTriggerEffects(DualSenseTriggerType triggerType) {
//             SetDualSenseTriggerEffect(triggerType, new DualSenseTriggerEffectOff());
//         }
//
//         static void SetDualSenseTriggerEffect(DualSenseTriggerType triggerType, IDualSenseTriggerEffect effect) {
//             if (!RewiredHelper.IsDualSense || !World.Only<AdaptiveTriggers>().Enabled) return;
//
//             if (ReInput.controllers.GetLastActiveController().extension is IDualSenseExtension dualSenseExtension) {
//                 dualSenseExtension.SetTriggerEffect(triggerType, effect);
//             }
//         }
//
//         static void SetDualSenseDualShock4LightColor(Color color) {
//             if (!RewiredHelper.IsDualSense && !RewiredHelper.IsDualShock4) return;
//
//             var currentControllerExtension = ReInput.controllers.GetLastActiveController().extension;
//
//             if (currentControllerExtension is IDualShock4Extension extension) {
//                 extension.SetLightColor(color);
//             }
//         }
     }
    
    [Serializable]
    public struct VibrationData {
        [Range(0, 1)] public float strength;
        [Range(0, 1)] public float duration;
    }

    [Serializable]
    public struct TriggersVibrationData {
        public CastingHand handsAffected;
        public List<VibrationData> effects;
    }

    public enum DualSenseEffectType : byte {
        Resistance = 0,
        ResistanceSlope = 1,
        Vibration = 2
    }

    [Serializable]
    public class DualSenseEffectData {
        public DualSenseEffectType type;

        [ShowIf(nameof(IsNotVibration)), Range(2, 7)] public byte startPosition;
        [ShowIf(nameof(IsNotVibration)), Range(3, 8)] public byte endPosition;
        [ShowIf(nameof(IsVibration)), Range(1, 9)] public byte position;
        [ShowIf(nameof(IsVibration)), Range(1, 8)] public byte amplitude;
        [ShowIf(nameof(IsVibration))] public byte frequency;
        [ShowIf(nameof(IsResistance)), Range(1, 8)] public byte strength;
        [ShowIf(nameof(IsResistanceSlope)), Range(1, 8)] public byte startStrength;
        [ShowIf(nameof(IsResistanceSlope)), Range(2, 8)] public byte endStrength;

        bool IsResistance => type == DualSenseEffectType.Resistance;
        bool IsResistanceSlope => type == DualSenseEffectType.ResistanceSlope;
        bool IsVibration => type == DualSenseEffectType.Vibration;
        bool IsNotVibration => !IsVibration;

        // public IDualSenseTriggerEffect GetEffect() {
        //     return type switch {
        //         DualSenseEffectType.Resistance => new DualSenseTriggerEffectWeapon { startPosition = this.startPosition, endPosition = this.endPosition, strength = this.strength },
        //         DualSenseEffectType.ResistanceSlope => new DualSenseTriggerEffectSlopeFeedback { startPosition = this.startPosition, endPosition = this.endPosition, startStrength = this.startStrength, endStrength = this.endStrength },
        //         DualSenseEffectType.Vibration => new DualSenseTriggerEffectVibration { amplitude = this.amplitude, frequency = this.frequency, position = this.position },
        //         _ => throw new ArgumentOutOfRangeException()
        //     };
        // }
    }
}