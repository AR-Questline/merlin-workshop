using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Thievery;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class AutoPickpocketHeroInteractionUI : AutoStartHeroInteractionUI<PickpocketAction> {
        public AutoPickpocketHeroInteractionUI(IInteractableWithHero interactable, PickpocketAction action) : base(interactable, action) { }
    }
}