#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.TitleScreen.FileVerification {
    [UsesPrefab("TitleScreen/VFileIntegrityPanel")]
    public class VFileIntegrityPanel : View<FileIntegrityPanel> {
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Image progressBar;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            Target.ListenTo(FileIntegrityPanel.Events.ProgressChanged, RefreshProgress, this);
        }

        public void RefreshProgress(float progress) {
            text.text = LocTerms.UIFileIntegrityProgress.Translate(progress.ToString("P0"));
            progressBar.fillAmount = progress;
        }

        void Update() {
            Target.Update();
        }
    }
}
#endif