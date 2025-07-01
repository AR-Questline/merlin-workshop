using System.Globalization;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemWeightComponent : ItemSlotComponent {
        [SerializeField] TextMeshProUGUI text;

        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            text.text = item.Weight.ToString(CultureInfo.CurrentCulture);
            SetInternalVisibility(false);
        }
    }
}