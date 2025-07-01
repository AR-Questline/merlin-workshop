using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.TitleScreen.ShadersPreloading {
    [UsesPrefab("TitleScreen/VShadersPreloadingPanel")]
    public class VShadersPreloadingPanel : View<ShadersPreloadingPanel> {
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Image progressBar;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            Target.ListenTo(ShadersPreloadingPanel.Events.ProgressChanged, RefreshProgress, this);
        }

        public void RefreshProgress(float progress) {
            text.text = LocTerms.UIShadersPreloadingProgress.Translate(progress.ToString("P0"));
            progressBar.fillAmount = progress;
        }
    }
}