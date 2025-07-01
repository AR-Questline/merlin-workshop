using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;

namespace Awaken.TG.Main.Fights.Mounts {
    public partial class MountAction : AbstractLocationAction {
        public override ushort TypeForSerialization => SavedModels.MountAction;

        string _mountName;
        bool _mountControllerBlocked;
        
        public override InfoFrame InfoFrame2 => new(_mountName, false);
        public override string DefaultActionName => IsIllegal ? LocTerms.Steal.Translate() : LocTerms.Mount.Translate();
        public override bool IsIllegal => ParentModel.Element<MountElement>().IsIllegal;
        
        protected override void OnFullyInitialized() {
            MountElement mountElement = ParentModel.Element<MountElement>();
            _mountName = mountElement.MountName;
            ParentModel.ListenTo(MovingPlatform.Events.MovingPlatformAdded, OnMovingPlatformAdded, this);
            base.OnFullyInitialized();
        }
        
        void OnMovingPlatformAdded(MovingPlatform movingPlatform) {
            movingPlatform.ListenTo(MovingPlatform.Events.MovingPlatformStateChanged, OnPlatformStateChanged, this);
        }

        void OnPlatformStateChanged(bool isMoving) {
            _mountControllerBlocked = isMoving;
            World.Any<HeroInteractionUI>()?.TriggerChange();
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (interactable is Location location && location.TryGetElement(out MountElement mount) && CanHeroInteract(hero)) {
                mount.Mount(hero);
            }
        }
        
        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (_mountControllerBlocked) {
                return ActionAvailability.Disabled;
            }
            
            if (interactable is Location location && location.TryGetElement(out MountElement mount) && CanHeroInteract(hero)) {
                return !mount.CanPetHorse() ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
            } 
                
            return ActionAvailability.Disabled;
        }

        bool CanHeroInteract(Hero hero) {
            return hero.Grounded || !hero.IsUnderWater;
        }
    }
}