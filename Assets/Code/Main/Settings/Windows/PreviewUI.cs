using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Settings.Windows {
    [SpawnsView(typeof(VPreviewUI))]
    public class PreviewUI : Element<AllSettingsUI> {
        
        public void UpdatePreview(VFocusableSetting setting) {
            View<VPreviewUI>()?.UpdatePreview(setting);
        }
    }
}
