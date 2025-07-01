using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemQuantityComponent : ItemSlotComponent {
        [SerializeField] bool showAlways;
        [SerializeField] TextMeshProUGUI text;

        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            text.text = item.Quantity.ToString();
            bool showAmount = showAlways || item.Quantity != 1;
            SetInternalVisibility(showAmount);
        }

        public void SetColor(Color color) {
            text.color = color;
        }
    }
}