using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Rewired;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options {
    public class KeyBindingOption : PrefOption {
        public override Type ViewType => typeof(VKeyBinding);

        public InputAction Action { get; }
        
        readonly BindingData _originalBinding;
        BindingData _previousBinding;
        BindingData _currentBinding;
        ActionElementMap _cachedActionElementMap;
        Guid _controllerIdentifier;
        
        public Action onChange;
        public ToggleOption toggleOptionIsToggle;
        
        public Guid ControllerIdentifier => _controllerIdentifier;
        public ActionElementMap CachedActionElementMap => _cachedActionElementMap;
        public Pole AxisContribution => _cachedActionElementMap.axisContribution;

        public BindingData CurrentBinding => _currentBinding;
        public override bool WasChanged => _currentBinding != _previousBinding || (toggleOptionIsToggle?.WasChanged ?? false);
        public bool IsOriginal => _currentBinding == _originalBinding;
        
        public KeyBindingOption(ControllerType controller, InputAction action, ActionElementMap actionElementMap, Guid controllerIdentifier) 
            : base(action.name, ConstructName(action, actionElementMap.axisContribution, controller), true) {
            // Action = action;
            // _cachedActionElementMap = actionElementMap;
            // _controllerIdentifier = controllerIdentifier;
            // _currentBinding = _previousBinding = _originalBinding = new BindingData(controller, actionElementMap.elementIdentifierId, actionElementMap.keyCode, actionElementMap.elementType, actionElementMap.axisContribution);
        }

        public override void ForceChange() {
            onChange?.Invoke();
            toggleOptionIsToggle?.ForceChange();
        }

        public override void Apply() {
            _previousBinding = _currentBinding;
            toggleOptionIsToggle?.Apply();
        }

        public override void Cancel() {
            ChangeBinding(_previousBinding);
            toggleOptionIsToggle?.Cancel();
        }

        public override void RestoreDefault() {
            ChangeBinding(_originalBinding);
            toggleOptionIsToggle?.RestoreDefault();
        }

        public void ChangeBinding(in BindingOverride bindingOverride) {
            ChangeBinding(bindingOverride.Binding, bindingOverride.ControllerIdentifier);
        }

        public void ChangeBinding(in BindingData binding, Guid guid = default) {
            // foreach (var actionName in OverlappingControlsUtil.GetMergeGroup(Action.name)) {
            //     List<ActionElementMap> maps = ActionElementMapFor(_currentBinding.Controller, actionName, AxisContribution, guid).ToList();
            //     for (int i = maps.Count - 1; i >= 0; i--) {
            //         var map = maps[i];
            //         if (map != null && map.controllerMap.enabled) {
            //             ChangeBinding(map, binding, out var newMap);
            //             if (newMap != null && map == _cachedActionElementMap && newMap.controllerMap.enabled) {
            //                 _cachedActionElementMap = newMap;
            //             }
            //         }
            //     }
            // }
            // _currentBinding = binding;
            // onChange?.Invoke();
        }

        void ChangeBinding(ActionElementMap map, in BindingData binding, out ActionElementMap newMap) {
            newMap = null;
            // if (_currentBinding.Controller == binding.Controller) {
            //     if (binding.TryGetForKeyboard(out KeyCode keyCode)) {
            //         ReplaceElementMap(map, keyCode, out newMap);
            //     } else if (binding.TryGetForMouse(out int elementIdentifierId)) {
            //         ReplaceElementMap(map, elementIdentifierId, out newMap);
            //     } else if (binding.TryGetForJoystick(out int joystickElementIdentifierId)) {
            //         ReplaceElementMap(map, joystickElementIdentifierId, binding.elementType, out newMap);
            //     }
            // } else if (map.controllerMap.enabled) {
            //     map.controllerMap.DeleteElementMap(map.id);
            //         
            //     if (binding.TryGetForKeyboard(out KeyCode keyCode)) {
            //         CreateElementMap(binding.Controller, map, keyCode, out newMap);
            //     } else if (binding.TryGetForMouse(out int elementIdentifierId)) {
            //         CreateElementMap(binding.Controller, map, elementIdentifierId, out newMap);
            //     } else if (binding.TryGetForJoystick(out int joystickElementIdentifierId)) {
            //         CreateElementMap(binding.Controller, map, joystickElementIdentifierId, out newMap);
            //     }
            // }
        }

        // static ControllerMap MapFor(ControllerType controller) {
        //     return RewiredHelper.Player.controllers.maps.GetAllMaps(controller).First();
        // }
        
        // static IEnumerable<ActionElementMap> ActionElementMapFor(ControllerType controller, string actionName, Pole axisContribution, Guid hardwareId) {
        //     int actionId = ReInput.mapping.GetAction(actionName).id;
        //     
        //     IEnumerable<ControllerMap> controllerMaps = controller == ControllerType.Joystick ? 
        //         RewiredHelper.Player.controllers.maps.GetAllMaps().Where(controllerMap => controllerMap.hardwareGuid == (hardwareId == default ? RewiredHelper.CurrentHardwareTypeGuid : hardwareId)) : 
        //         RewiredHelper.Player.controllers.maps.GetFirstElementMapWithAction(controller, actionId, true).controllerMap.Yield();
        //
        //     return controllerMaps.SelectMany(controllerMap => controllerMap.AllMaps.Where(actionElementMap => actionElementMap.actionId == actionId && actionElementMap.axisContribution == axisContribution));
        // }
        
        // static void ReplaceElementMap(ActionElementMap actionElementMap, KeyCode keyCode, out ActionElementMap newActionElementMap) {
        //     actionElementMap.controllerMap.ReplaceElementMap(
        //         actionElementMap.id,
        //         actionElementMap.actionId,
        //         actionElementMap.axisContribution,
        //         keyCode,
        //         actionElementMap.modifierKeyFlags,
        //         out newActionElementMap
        //     );
        // }
        
        // static void ReplaceElementMap(ActionElementMap actionElementMap, int elementIdentifierId, out ActionElementMap newActionElementMap) {
        //     actionElementMap.controllerMap.ReplaceElementMap(
        //         actionElementMap.id,
        //         actionElementMap.actionId,
        //         actionElementMap.axisContribution,
        //         elementIdentifierId,
        //         actionElementMap.elementType,
        //         actionElementMap.axisRange,
        //         actionElementMap.invert,
        //         out newActionElementMap
        //     );
        // }
        
        // static void ReplaceElementMap(ActionElementMap actionElementMap, int elementIdentifierId, ControllerElementType controllerElementType, out ActionElementMap newActionElementMap) {
        //     actionElementMap.controllerMap.ReplaceElementMap(
        //         actionElementMap.id,
        //         actionElementMap.actionId,
        //         actionElementMap.axisContribution,
        //         elementIdentifierId,
        //         controllerElementType,
        //         actionElementMap.axisRange,
        //         actionElementMap.invert,
        //         out newActionElementMap
        //     );
        // }

        // static void CreateElementMap(ControllerType controller, ActionElementMap currentActionElementMap, KeyCode keyCode, out ActionElementMap newActionElementMap) {
        //     MapFor(controller).CreateElementMap(
        //         currentActionElementMap.actionId, 
        //         currentActionElementMap.axisContribution, 
        //         keyCode, 
        //         currentActionElementMap.modifierKeyFlags,
        //         out newActionElementMap
        //     );  
        // }

        // static void CreateElementMap(ControllerType controller, ActionElementMap currentActionElementMap, int elementIdentifierId, out ActionElementMap newActionElementMap) {
        //     MapFor(controller).CreateElementMap(
        //         currentActionElementMap.actionId,
        //         currentActionElementMap.axisContribution,
        //         elementIdentifierId,
        //         currentActionElementMap.elementType,
        //         currentActionElementMap.axisRange,
        //         currentActionElementMap.invert,
        //         out newActionElementMap
        //     );
        // }

        static string ConstructName(InputAction action, Pole pole, ControllerType controller) {
            // if (action.type == InputActionType.Axis && controller != ControllerType.Joystick) {
            //     string axisDesc = pole == Pole.Positive ? action.positiveDescriptiveName : action.negativeDescriptiveName;
            //     return axisDesc.Translate();
            // } else {
            //     return action.descriptiveName.Translate();
            // }
            return "";
        }
    }
}