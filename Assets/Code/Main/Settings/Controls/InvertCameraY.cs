using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Rewired;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class InvertCameraY : Setting, IRewiredSetting {
        ToggleOption _invert;
        
        public sealed override string SettingName => LocTerms.SettingsCameraInvert.Translate();
        public override IEnumerable<PrefOption> Options => _invert.Yield();

        public InvertCameraY() {
            _invert = new("Settings_CameraInvert", SettingName, false, true);
        }

        protected override void OnInitialize() {
            ModelUtils.ListenToFirstModelOfType(Focus.Events.ControllerChanged, Set, this);
        }

        protected override void OnApply() {
            Set();
        }

        void Set() {
            bool value = _invert.Enabled;
            AssignFor(ControllerType.Mouse, !value);
            AssignFor(ControllerType.Joystick, !value);
        }

        void AssignFor(ControllerType controller, bool value) {
            // ActionElementMap actionMap = GetActionMap(controller, out var map);
            // if (actionMap == null) return;
            
            // ElementAssignment assignment = ElementAssignment.CompleteAssignment(controller, actionMap.elementType, 
            //     actionMap.elementIdentifierId, actionMap.axisRange, actionMap.keyCode,
            //     actionMap.modifierKeyFlags, actionMap.actionId, actionMap.axisContribution, value,
            //     elementMapId: actionMap.id);
            // map.ReplaceElementMap(assignment);
        }

        // ActionElementMap GetActionMap(ControllerType controller, out ControllerMap map) {
        //     InputAction action = ReInput.mapping.GetAction(KeyBindings.Gameplay.CameraVertical);
        //     
        //     map = RewiredHelper.Player.controllers.maps.GetFirstElementMapWithAction(controller, action.id, true)?.controllerMap;
        //     return map?.AllMaps.FirstOrDefault(m => m.actionId == action.id);
        // }
    }
}