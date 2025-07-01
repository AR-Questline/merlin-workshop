using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Locations.Shops.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    [UsesPrefab("Story/PopupContents/" + nameof(VInputShopItemQuantityUI))]
    public class VInputShopItemQuantityUI : VInputItemQuantityUI {
        [SerializeField] GameObject pricePreviewParent;
        [SerializeField] Image coinIcon;
        [SerializeField] TextMeshProUGUI priceText;
        
        int _maxAffordableItemsQuantity;
        
        bool CanAfford => Target.Value <= _maxAffordableItemsQuantity;
        ShopVendorBaseUI Vendor { get; set; }

        protected override void OnInitialize() {
            Vendor = Target.ParentModel as ShopVendorBaseUI;
            _maxAffordableItemsQuantity = Vendor!.AffordableItemsAmount();
            priceText.SetText(GetTotalPrice());
            
            base.OnInitialize();
        }

        protected override void OnValueChanged(int newValue) {
            base.OnValueChanged(newValue);
            priceText.SetText(GetTotalPrice(newValue));
            ColorComponents();
        }

        protected override void ColorComponents() {
            var targetColor = CanAfford ? ARColor.MainWhite : ARColor.MainRed;
            quantityText.DOColor(targetColor, FadeDuration).SetUpdate(true).SetEase(Ease.OutBack);
            priceText.DOColor(targetColor, FadeDuration).SetUpdate(true).SetEase(Ease.OutBack);
            coinIcon.DOColor(targetColor, FadeDuration).SetUpdate(true).SetEase(Ease.OutBack);
        }

        string GetTotalPrice(int amount = 1) {
            return TradeUtils.Price(Vendor!.Seller, Vendor.Buyer, Target.Item, amount).ToString();
        }
    }
}