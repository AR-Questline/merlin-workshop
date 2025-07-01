using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemEquippedComponent : ItemSlotComponent {
        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            if (TryHandleWeaponLoadoutContext(item)) {
                return;
            }
            
            if (TryHandleEquipmentChooseContext(item)) {
                return;
            }
            
            SetInternalVisibility(item.IsEquipped);
        }

        /// <summary>
        /// Hacky solution for marking a weapon as equipped per weapon's loadout context.
        /// </summary>
        bool TryHandleWeaponLoadoutContext(Item item) {
            LoadoutsUI loadouts = World.Any<LoadoutsUI>();
            var weaponLoadoutSlot = loadouts?.CurrentlyChangedLoadout;
            
            if (weaponLoadoutSlot) {
                SetInternalVisibility(item.IsUsedInLoadout(weaponLoadoutSlot.LoadoutIndex));
                return true;
            }
            
            if (loadouts && item.IsUsedInLoadout(out HeroLoadout _)) {
                SetInternalVisibility(true);
                return true;
            }
    
            return false;
        }
        
        bool TryHandleEquipmentChooseContext(Item item) {
            if (World.Any<LoadoutsUI>() == null) {
                return false;
            }
            
            bool isEquippedInOtherSlot = item.IsEquipped && Hero.Current.Inventory.SlotWith(item) != World.Any<EquipmentChooseUI>()?.EquipmentSlotType;
            SetInternalVisibility(!isEquippedInOtherSlot);
            return isEquippedInOtherSlot;
        }
    }
}