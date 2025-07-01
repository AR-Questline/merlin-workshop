using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.UI;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemTheftComponent : ItemSlotComponent {
        [SerializeField] Image icon;
        
        void Start() {
            SetExternalVisibility(true);
        }
        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            if (icon == null) {
                Log.Important?.Error("Theft icon component is attached but icon field is null!");
                return;
            }

            SetInternalVisibility(StolenItemElement.IsStolen(item));
        }
    }
}