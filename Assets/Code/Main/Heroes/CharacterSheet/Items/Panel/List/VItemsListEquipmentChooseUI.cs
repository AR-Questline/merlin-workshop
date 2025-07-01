using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Gems.GemManagement;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VItemsListEquipmentChooseUI))]
    public class VItemsListEquipmentChooseUI : VItemsListDefaultUI {
        protected override void ConfigureList() {
            MaxColumnCount = 5;
            
            if (Target.ParentModel.Config is EquipmentChooseUI chooseUI) {
                var slotType = chooseUI.EquipmentSlotType;
                MaxRowCount = EquipmentSlotType.Armors.Contains(slotType) ? 5 : 6;
            }
            else if (Target.ParentModel.Config is GemChooseUI) {
                MaxRowCount = 5;
            }
            else {
                MaxRowCount = 6;
            }
            
            base.ConfigureList();
        }
    }
}