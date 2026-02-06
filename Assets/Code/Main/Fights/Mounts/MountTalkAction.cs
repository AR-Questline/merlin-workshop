using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.Mounts {
    public partial class MountTalkAction : DialogueAction {
        public override ushort TypeForSerialization => SavedModels.MountTalkAction;
        
        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (interactable is Location location && location.TryGetElement(out MountElement mount) && CanHeroInteract(hero) && !World.HasAny<SaveBlocker>()) {
                return mount.CanPetHorse() ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
            }

            return ActionAvailability.Disabled;
        }
        
        bool CanHeroInteract(Hero hero) {
            return hero.Grounded && !hero.IsUnderWater;
        }
    }
}