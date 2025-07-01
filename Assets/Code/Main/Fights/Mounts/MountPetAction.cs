using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public partial class MountPetAction : AbstractLocationAction {
        public override ushort TypeForSerialization => SavedModels.MountPetAction;

        public override string DefaultActionName => LocTerms.Pet.Translate();

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (interactable is Location location && location.TryGetElement(out MountElement mount) && CanHeroInteract(hero)) {
                return mount.CanPetHorse() ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
            }

            return ActionAvailability.Disabled;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (CanHeroInteract(hero)) {
                hero.Trigger(Hero.Events.HideWeapons, true);
                int mountInteractionIndex = Random.Range(0, 2);
                var mountInteractionEvent = mountInteractionIndex == 0 ? ToolInteractionFSM.Events.PatMount : ToolInteractionFSM.Events.PetMount;
                hero.Trigger(mountInteractionEvent, hero);
            }
        }
        
        bool CanHeroInteract(Hero hero) {
            return hero.Grounded && !hero.IsUnderWater;
        }
    }
}