using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing {
    [UsesPrefab("UI/Housing/" + nameof(VDecorMode))]
    public class VDecorMode : View<DecorMode> {
        [SerializeField] TextMeshProUGUI decorModeText;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            decorModeText.SetText(LocTerms.HousingDecorMode.Translate());
        }
    }
}