using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Actions {
    public abstract partial class AbstractHeroAction<T> : Element<T>, IHeroActionModel where T : class, IModel {
        [Saved] bool _disabled = false;
        bool _isInteracting;
        bool _finishAfterInteracting;

        // === Properties
        string RequiresItemMessage => !HeroHasRequiredItem() ? $"{DefaultActionName} ({ShowInfo()})" : DefaultActionName;
        protected ItemRequirement ItemRequirement => ParentModel.GetModelInParent<Location>()?.TryGetElement<ItemRequirement>();
        protected virtual InteractRunType RunInteraction => InteractRunType.AfterRun;

        public virtual bool IsIllegal => false;
        public virtual InfoFrame ActionFrame => new(RequiresItemMessage, HeroHasRequiredItem());
        public virtual InfoFrame InfoFrame1 => InfoFrame.Empty;
        public virtual InfoFrame InfoFrame2 => InfoFrame.Empty;
        public virtual string DefaultActionName => LocTerms.Interact.Translate();

        protected bool Disabled {
            get => _disabled;
            set {
                if (_disabled == value) return;
                _disabled = value;
                if (_disabled) {
                    OnDisabled();
                } else {
                    OnEnabled();
                }
            }
        }

        protected enum InteractRunType {
            BeforeRun,
            AfterRun,
            DontRun
        };

        // === Default implementations

        public virtual ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            bool disableByItemRequirement = ItemRequirement?.ShouldDisableInteraction(hero) ?? false;
            return disableByItemRequirement || Disabled || IsBlocked(hero, interactable)
                ? ActionAvailability.Disabled
                : ActionAvailability.Available;
        }

        bool IsBlocked(Hero hero, IInteractableWithHero interactable) {
            // interactable can be different that ParentModel in case of NpcPresence
            return HasActionBlocker(ParentModel) || (interactable != ParentModel && HasActionBlocker(interactable));
            
            bool HasActionBlocker(object obj) {
                return obj is Location location && location.Elements<IHeroActionBlocker>().Any(b => b.IsBlocked(hero, interactable));
            }
        }
        
        public virtual IHeroInteractionUI InteractionUIToShow(IInteractableWithHero interactable) {
            return IsIllegal
                       ? new HeroIllegalInteractionUI(interactable)
                       : new HeroInteractionUI(interactable);
        }

        public void DisableAction() => Disabled = true;
        public void EnableAction() => Disabled = false;
        protected virtual void OnEnabled(){}
        protected virtual void OnDisabled(){}

        public bool StartInteraction(Hero hero, IInteractableWithHero interactable) {
            if (ItemRequirement != null && !ItemRequirement.ConsumeItem(hero)) {
                return false;
            }

            _isInteracting = true;
            if (RunInteraction == InteractRunType.BeforeRun) {
                Interact(hero, interactable);
                OnStart(hero, interactable);
            } else if (RunInteraction == InteractRunType.AfterRun) {
                OnStart(hero, interactable);
                Interact(hero, interactable);
            } else {
                OnStart(hero, interactable);
            }
            _isInteracting = false;
            
            if (_finishAfterInteracting) {
                _finishAfterInteracting = false;
                FinishInteraction(hero, interactable);
            }
            
            return true;
        }

        public void FinishInteraction(Hero hero, IInteractableWithHero interactable) {
            if (_isInteracting) {
                _finishAfterInteracting = true;
                return;
            }
            
            OnFinish(hero, interactable);
            if (interactable is Location location) {
                location.Trigger(Location.Events.InteractionFinished, new LocationInteractionData(hero, location));
            }
        }

        public void EndInteraction(Hero hero, IInteractableWithHero interactable) {
            if (HasBeenDiscarded) {
                return;
            }
            OnEnd(hero, interactable);
            VGUtils.SendCustomEvent(interactable.InteractionVSGameObject, hero.ParentTransform.gameObject, VSCustomEvent.InteractEnd);
        }

        protected virtual void OnStart(Hero hero, IInteractableWithHero interactable) { }
        protected virtual void OnFinish(Hero hero, IInteractableWithHero interactable) { }
        protected virtual void OnEnd(Hero hero, IInteractableWithHero interactable) { }

        protected static void Interact(ICharacter character, IInteractableWithHero interactable) {
            if (interactable is Location location) {
                var data = new LocationInteractionData(character, location);
                location.Trigger(Location.Events.Interacted, data);
                location.Trigger(Location.Events.AfterInteracted, data);
            }
            VGUtils.SendCustomEvent(interactable.InteractionVSGameObject, character?.ParentTransform.gameObject, VSCustomEvent.Interact);
        }

        protected bool HeroHasRequiredItem() {
            if (ItemRequirement) {
                ItemRequirement.RefreshValues(Hero.Current);
                return ItemRequirement.HasItem;
            }

            return true;
        }

        string ShowInfo() {
            if (ItemRequirement) {
                return ItemRequirement.ShowInfo ? ItemRequirement.Info : string.Empty;
            }

            return string.Empty;
        }
    }

    public abstract partial class AbstractLocationAction : AbstractHeroAction<Location> { }
}