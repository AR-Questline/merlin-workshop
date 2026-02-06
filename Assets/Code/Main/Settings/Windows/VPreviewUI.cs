using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Windows {
    [UsesPrefab("Settings/VPreviewUI")]
    public class VPreviewUI : View<PreviewUI> {
        [SerializeField] Image preview;
        [SerializeField] TextMeshProUGUI previewText;

        public override Transform DetermineHost() => Target.ParentModel.View<VSettingsUI>().previewParent.transform;

        public void UpdatePreview(VFocusableSetting setting) {
            setting.GenericOption.GetPreview.Invoke().RegisterAndSetup(this, preview);
            previewText.text = setting.GenericOption.GetPreviewText.Invoke();
        }
    }
}
