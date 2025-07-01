using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCPricePreview : ViewComponent<IWithPricePreview> {
        [SerializeField] TextMeshProUGUI costNameText;
        [SerializeField] TextMeshProUGUI costValueText;

        static Color CanAffordColor => ARColor.MainWhite;
        static Color CantAffordColor => ARColor.MainGrey;

        protected override void OnAttach() {
            costNameText.SetText(LocTerms.Cost.Translate());
            Target.ListenTo(IWithPricePreview.Events.PriceRefreshed, Refresh, this);
            Refresh();
        }

        void Refresh() {
            costValueText.SetText(Target.Price.ToString());
            costValueText.color = Target.CanAfford ? CanAffordColor : CantAffordColor;
        }
    }
}