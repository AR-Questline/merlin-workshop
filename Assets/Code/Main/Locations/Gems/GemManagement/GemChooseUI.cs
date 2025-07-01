using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public partial class GemChooseUI : ItemChooseUI<GemManagementUI>, IClosable {
        readonly GemSlotUI _gemSlotUI;
        
        public override Type ItemsListElementView => typeof(VItemGemChooseElement);

        public GemChooseUI(GemSlotUI gemSlotUI) : base(ItemsTabType.All.Yield()) {
            _gemSlotUI = gemSlotUI;
        }

        protected override void HoveredItemsChanged(Item item) {
            _promptSelect.SetActive(item != null); 
        }

        protected override void Choose(Item item) {
            _gemSlotUI.SetPreviewGemItem(item);
            Close();
        }

        protected override bool ItemFilter(Item item) {
            var gearItem = ParentModel.ClickedItem;
            var gemSlots = ParentModel.Elements<GemSlotUI>();

            if (gemSlots.Any(gem => gem.PreviewGemItem == item)) {
                return false;
            }
            
            if (gearItem.IsArmor) {
                return item.IsArmorGem;
            }

            if (gearItem.IsWeapon) {
                return item.IsWeaponGem;
            }
            
            return false;
        }

        public void Close() {
            ParentModel.ParentModel.ShowEmptyInfo(true);
            Discard();
        } 
    }
}