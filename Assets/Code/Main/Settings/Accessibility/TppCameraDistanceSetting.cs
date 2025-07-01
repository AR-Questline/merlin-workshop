using System.Collections.Generic;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class TppCameraDistanceSetting : Setting {
        const string PrefId = "Accessibility_TppCameraDistance";
        const float DefaultCameraDistance = 1.5f;
        const float MinCameraDistance = 1.5f;
        const float MaxCameraDistance = 4.5f;
        const float CameraDistanceStep = 0.1f;
        
        SliderOption CameraDistanceOption { get; set; }
        public sealed override string SettingName => LocTerms.SettingsTppPerspectiveCameraDistance.Translate();
        public override IEnumerable<PrefOption> Options => CameraDistanceOption.Yield();
        public float TppCameraDistance => CameraDistanceOption.Value;

        public TppCameraDistanceSetting() {
            CreateCameraDistanceOption();
        }

        protected override void OnInitialize() {
            World.Any<CheatController>()?.ListenTo(Model.Events.AfterChanged, CreateCameraDistanceOption, this);
        }
        
        void CreateCameraDistanceOption() {
            var maxCameraDistance = CheatController.CheatsEnabled() ? MaxCameraDistance * 10 : MaxCameraDistance;
            CameraDistanceOption = new SliderOption(PrefId, SettingName, MinCameraDistance, maxCameraDistance, false,
                "{0:0.0}", DefaultCameraDistance, true, CameraDistanceStep);
        }

        public void ChangeValue(float delta) {
            CameraDistanceOption.Value -= delta;
            Apply(out _);
        }
    }
}