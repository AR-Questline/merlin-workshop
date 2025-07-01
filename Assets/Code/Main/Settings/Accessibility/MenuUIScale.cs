using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI;
using Awaken.TG.MVC;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class MenuUIScale : CanvasScaleSetting {
        public sealed override string SettingName => LocTerms.SettingsMenuUIScale.Translate();
        protected override float MaxScale => 100f;

        public MenuUIScale() {
            MainSliderOption = CreateSliderOption("Accessibility_MenuUIScale", SettingName);
            MainSliderOption.AddTooltip(LocTerms.MenuUIScaleSettingTooltip.Translate);
        }

        protected override void UpdateSettings() {
            ResizeCanvasScaler(World.Services.Get<CanvasService>().MainCanvas);
        }
    }
}