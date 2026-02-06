using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.TitleScreen.SaveVerifications {
    [UsesPrefab("TitleScreen/VSaveVerificationPanel")]
    public class VSaveVerificationPanel : View<SaveVerificationPanel> {
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Image progressBar;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            Target.Progress.ProgressChanged += RefreshProgress;
        }

        void RefreshProgress(object _, float progress) {
            text.text = LocTerms.UISaveVerificationProgress.Translate(progress.ToString("P0"));
            progressBar.fillAmount = progress;
        }

        protected override IBackgroundTask OnDiscard() {
            Target.Progress.ProgressChanged -= RefreshProgress;
            return base.OnDiscard();
        }

        protected void OnDestroy() {
            if (GenericTarget != null) {
                Target.Progress.ProgressChanged -= RefreshProgress;
            }
        }
    }
}
