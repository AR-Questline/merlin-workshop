using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Locations.Gems {
    public class VCServicePricePreview : ViewComponent<IGemBase> {
        const float FadeDuration = 0.3f;
        
        [SerializeField] CurrencyType currencyType;
        [SerializeField] Color cantAffordColor;
        [SerializeField] TextMeshProUGUI costText;
        [SerializeField] TextMeshProUGUI priceText;
        [SerializeField] Image coinIcon;

        protected override void OnAttach() {
            Target.ListenTo(IGemBase.Events.GemActionPerformed, Refresh, this);
            Target.ListenTo(IGemBase.Events.CostRefreshed, Refresh, this);
            Target.ListenTo(IGemBase.Events.ClickedItemChanged, item => Refresh(item != null), this);
            Refresh(true);

            if (costText) {
                costText.SetText($"{LocTerms.Cost.Translate()}:");
            }
        }

        void Refresh(bool instant = false) {
            priceText.SetText(currencyType == CurrencyType.Money ? Target.ServiceCost.ToString() : Target.CobwebServiceCost.ToString());
            var targetColor = Target.CanAfford(currencyType) ? ARColor.LightGrey : cantAffordColor;
            priceText.DOColor(targetColor, instant ? FadeDuration : 0f).SetUpdate(true).SetEase(Ease.OutBack);
            coinIcon.DOColor(targetColor, instant ? FadeDuration : 0f).SetUpdate(true).SetEase(Ease.OutBack);
        }
    }
}