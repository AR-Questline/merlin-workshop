using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class GeneralGraphicsXbox : GeneralGraphics {
        const int QualityIndex = 0;
        const int PerformanceIndex = 1;

        protected override void OnApply() {
            base.OnApply();
            QualitySettings.SetQualityLevel(Option.OptionInt == 0 ? PerformanceIndex : QualityIndex);
            World.Only<TextureQuality>().OnQualitySettingsChanged();
            World.Any<ScreenResolution>()?.Refresh(); // we need to call this to update VSyncCount that is overriden by QualitySettings change
        }
    }
}