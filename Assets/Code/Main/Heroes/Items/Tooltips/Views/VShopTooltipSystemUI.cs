using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Views {
    [UsesPrefab("Items/TooltipSystem/" + nameof(VShopTooltipSystemUI))]
    public class VShopTooltipSystemUI : VBagItemTooltipSystemUI {
        [SerializeField] TextMeshProUGUI cantAffordText;
        [SerializeField] GameObject cantAffordSection;

        public void SetCantAffordText(string text) {
            cantAffordSection.SetActive(!string.IsNullOrEmpty(text));
            cantAffordText.SetText(text);
        }
    }
}