using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class AimDeadzone : Setting {
        const int RightAnalogXAxisIndex = 2;
        const int RightAnalogYAxisIndex = 3;
        
        SliderOption Option { get; }
        
        public sealed override string SettingName => PlatformUtils.IsMicrosoft 
            ? LocTerms.GamepadDeadZoneMicrosoft.Translate()
            : LocTerms.GamepadDeadZone.Translate();
        
        public override IEnumerable<PrefOption> Options => RewiredHelper.IsGamepad ? Option.Yield() : Enumerable.Empty<PrefOption>();
        public float Value => Option.Value;
        
        // === Constructor
        public AimDeadzone(float defaultDeadzone = 0.15f) {
            Option = new SliderOption("Setting_AimDeadzone", SettingName, 0.05f, 0.3f, false, NumberWithPercentFormat, defaultDeadzone, true, 0.01f);
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, () => SetDeadZone(Value));
        }

        protected override void OnApply() {
            SetDeadZone(Option.Value);
        }

        void SetDeadZone(float value) {
            // var controller =  RewiredHelper.Player.controllers.GetLastActiveController();
            // if (controller is ControllerWithAxes controllerWithAxes) {
            //     CalibrationMap calibrationMap = controllerWithAxes.calibrationMap;
            //     var xLookInput = calibrationMap.GetAxis(RightAnalogXAxisIndex);
            //     var yLookInput = calibrationMap.GetAxis(RightAnalogYAxisIndex);
            //     xLookInput.deadZone = value;
            //     yLookInput.deadZone = value;
            // }
        }
    }
}
