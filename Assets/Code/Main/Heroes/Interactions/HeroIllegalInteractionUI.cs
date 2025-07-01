using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Heroes.Interactions {
    [SpawnsView(typeof(VHeroInteractionUI))]
    public partial class HeroIllegalInteractionUI : HeroInteractionHoldUI {
        float IllegalHoldTime => ButtonsHandler.HoldTime * ParentModel.HeroStats.TheftHoldTimeModifier;
        public override float HoldTime => IllegalHoldTime;

        public override bool HeldButton {
            get => _heldButton;
            set {
                _heldButton = value;
                if (_heldButton) {
                    Hero.Current.Element<IllegalActionTracker>().PerformingSuspiciousInteraction();
                }
            }
        }

        IllegalActionTracker _illegalActionTracker;

        public HeroIllegalInteractionUI(IInteractableWithHero interactable) : base(interactable) { }
    }
}