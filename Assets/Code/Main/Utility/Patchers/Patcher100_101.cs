using System;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher100_101 : Patcher {
        protected override Version MaxInputVersion => new(1, 0, 9999);
        protected override Version FinalVersion => new(1, 1, 0);

        public override void AfterRestorePatch() {
            if (PlatformUtils.IsConsole) {
                var preset = World.Any<GraphicPresets>()?.ActivePreset;
                if (preset != null && preset != Preset.Custom) {
                    var vegetation = World.Any<Vegetation>();
                    if (vegetation) {
                        vegetation.SetValueForPreset(preset);
                        vegetation.Apply(out _);
                    }
                }
                if (PlatformUtils.IsXboxScarlett) {
                    var screenResolution = World.Any<ScreenResolution>();
                    if (screenResolution != null) {
                        screenResolution.VSyncOption.Enabled = false;
                        screenResolution.Apply(out _);
                    }
                }
            }
        }
    }
}