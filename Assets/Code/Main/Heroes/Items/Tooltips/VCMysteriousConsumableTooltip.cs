using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public class VCMysteriousConsumableTooltip : ViewComponent<ExperimentalCooking> {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI effectText;
        [SerializeField] TextMeshProUGUI typeText;

        protected override void OnAttach() {
            nameText.SetText(LocTerms.Unknown.Translate());
            effectText.SetText(LocTerms.Unknown.Translate());
            typeText.SetText(LocTerms.Consumable.Translate());
        }
    }
}