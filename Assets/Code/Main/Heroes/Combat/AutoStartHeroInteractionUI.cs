using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Combat {
    public abstract partial class AutoStartHeroInteractionUI<T> : Element<Hero>, IHeroInteractionUI where T : IHeroAction {
        protected readonly T action;
        
        public sealed override bool IsNotSaved => true;
        public virtual bool Visible => true;
        public virtual IInteractableWithHero Interactable { get; }

        public AutoStartHeroInteractionUI(IInteractableWithHero interactable, T action) {
            this.action = action;
            Interactable = interactable;
        }
        
        protected override void OnFullyInitialized() {
            action.StartInteraction(ParentModel, Interactable);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            action.EndInteraction(ParentModel, Interactable);
        }
    }
}