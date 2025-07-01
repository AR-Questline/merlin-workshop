using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public class VCMountHorseArmor : ViewComponent<Location> {
        IEventListener _onArmorSlotChanged;

        [SerializeField] Transform armorHost;
        [SerializeField] Transform noArmorHost;

        MountElement _mount;
        
        Hero Hero => Hero.Current;
        bool HorseOwnedByHero => Hero.OwnedMount.Get() == _mount;
        bool HorseArmorEquippedToHero => Hero.Inventory.EquippedItem(EquipmentSlotType.HorseArmor) != null;
        
        protected override void OnAttach() {
            base.OnAttach();

            _mount = Target.Element<MountElement>();
            
            Hero.ListenTo(MountElement.Events.HeroMounted, CheckMountOwnership, this);
            CheckMountOwnership();
        }
        
        void CheckMountOwnership() {
            if (HorseOwnedByHero) {
                _onArmorSlotChanged = Hero.Inventory.ListenTo(
                    ICharacterInventory.Events.SlotChanged(EquipmentSlotType.HorseArmor), UpdateArmorVisibility, this);
            } else {
                World.EventSystem.TryDisposeListener(ref _onArmorSlotChanged);
            }

            UpdateArmorVisibility();
        }

        void UpdateArmorVisibility() {
            bool shouldBeVisible = HorseOwnedByHero && HorseArmorEquippedToHero;
            armorHost.TrySetActiveOptimized(shouldBeVisible);
            noArmorHost.TrySetActiveOptimized(!shouldBeVisible);
        }

        protected override void OnDiscard() {
            base.OnDiscard();
            _onArmorSlotChanged = null;
        }
    }
}