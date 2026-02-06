using System.Globalization;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Locations.Gems {
    [UsesPrefab("Gems/" + nameof(VIdentifyUI))]
    public class VIdentifyUI : VGemBaseUI {
        [Title("Labels")]
        [SerializeField] TextMeshProUGUI itemNameText;
        [SerializeField] TextMeshProUGUI itemTypeText;
        [SerializeField] TextMeshProUGUI itemPriceText;
        [SerializeField] TextMeshProUGUI itemWeightText;
        [SerializeField] Image qualityImage;
        
        public void ResetOutcomeSection(Item item) {
            itemNameText.SetText(item.DisplayName);
            itemTypeText.SetText(ItemUtils.ItemTypeTranslation(item));
            itemPriceText.SetText(item.Price.ToString());
            itemWeightText.SetText(item.Weight.ToString(CultureInfo.InvariantCulture));
            qualityImage.color = item.Quality.BgColor.WithAlpha(qualityImage.color.a);
        }
    }
}