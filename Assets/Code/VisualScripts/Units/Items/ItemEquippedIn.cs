using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Items {
    [UnityEngine.Scripting.Preserve]
    public class ItemEquippedIn : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        [RichEnumExtends(typeof(EquipmentSlotType))]
        public RichEnumReference slot;
        
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            ValueOutput("item", flow => character.Value(flow).Inventory.EquippedItem(slot.EnumAs<EquipmentSlotType>()));
        }
    }
}