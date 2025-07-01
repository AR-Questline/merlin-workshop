using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class PlainFoodLevelComponent : ItemSlotComponent {
        [SerializeField] TextMeshProUGUI levelText;

        void Start() {
            SetExternalVisibility(true);
        }

        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            SetInternalVisibility(item.IsPlainFood && item.Level > 0);
            
            if (levelText) {
                levelText.text = $"+{item.Level.ModifiedInt}";
            }
        }
    }
}