using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemLevelComponent : ItemSlotComponent {
        [SerializeField] TextMeshProUGUI number;
        [SerializeField] Image arrow;

        Hero Hero => Hero.Current;

        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            bool isBetter = item.IsBetterThanEquipped(Hero);
            ARColor color = isBetter ? ARColor.MainGreen : ARColor.MainWhite;
            number.text = item.Level.ModifiedInt.ToString();
            number.faceColor = color;
            arrow.color = color;
            arrow.enabled = isBetter;
            SetInternalVisibility(true);
        }
    }
}