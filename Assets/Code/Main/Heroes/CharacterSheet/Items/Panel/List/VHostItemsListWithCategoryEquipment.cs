using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    [UsesPrefab("Items/List/" + nameof(VHostItemsListWithCategoryEquipment))]
    public class VHostItemsListWithCategoryEquipment : VHostItemsListWithCategory {
        const float HeightOf6Rows = 694;
        const float HeightOf5Rows = 582;
        
        [SerializeField] LayoutElement viewPortLayout;

        protected override void OnInitialize() {
            if (Target.ParentModel.Config is EquipmentChooseUI chooseUI) {
                var slotType = chooseUI.EquipmentSlotType;
                viewPortLayout.minHeight = EquipmentSlotType.Armors.Contains(slotType) ? HeightOf5Rows : HeightOf6Rows;
            }
        }
    }
}