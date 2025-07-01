using System.Globalization;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    [UsesPrefab("Gems/" + nameof(VIdentifyUI))]
    public class VIdentifyUI : VGemBaseUI {
        [Title("Labels")]
        [SerializeField] TextMeshProUGUI itemNameText;
        [SerializeField] TextMeshProUGUI itemTypeText;
        [SerializeField] TextMeshProUGUI itemPriceText;
        [SerializeField] TextMeshProUGUI itemWeightText;
        
        public void ResetOutcomeSection(Item item) {
            itemNameText.SetText(item.DisplayName);
            itemTypeText.SetText(ItemUtils.ItemTypeTranslation(item));
            itemPriceText.SetText(item.Price.ToString());
            itemWeightText.SetText(item.Weight.ToString(CultureInfo.InvariantCulture));
        }
    }
}