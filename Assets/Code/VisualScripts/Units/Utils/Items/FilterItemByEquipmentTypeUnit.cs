using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Utils.Items {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Utils/Items")]
    [UnitTitle("Filter Item By EquipmentType")]
    [UnityEngine.Scripting.Preserve]
    public class FilterItemByEquipmentTypeUnit : ARUnit {
        protected override void Definition() {
            var equipmentType = RequiredARValueInput<EquipmentType>("equipmentType");
            ValueOutput<Func<Item, bool>>("filter", flow => {
                var type = equipmentType.Value(flow);
                return item => item.TryGetElement(out ItemEquip equip) && equip.EquipmentType == type;
            });
        }
    }
}