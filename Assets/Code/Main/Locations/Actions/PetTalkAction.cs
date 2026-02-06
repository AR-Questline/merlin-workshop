using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class PetTalkAction : DialogueAction {
        public override ushort TypeForSerialization => SavedModels.PetTalkAction;

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (World.HasAny<SaveBlocker>()) {
                return ActionAvailability.Disabled;
            }
            return base.GetAvailability(hero, interactable);
        }
    }
}