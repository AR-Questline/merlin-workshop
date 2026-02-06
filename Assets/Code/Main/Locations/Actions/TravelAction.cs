using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Utility.UI;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class TravelAction : AbstractHeroAction<Portal> {
        public sealed override bool IsNotSaved => true;

        readonly string _interactLabel;

        protected override bool DisableInCombat => true;
        // Info frame bool decides only if the interaction button is visible is enabled, the true activity must be handled in Start / OnStart
        protected override InfoFrame ActionFrameInternal => !string.IsNullOrWhiteSpace(_interactLabel) 
                                                    ? new InfoFrame(_interactLabel, HeroHasRequiredItem()) 
                                                    : base.ActionFrameInternal;

        public TravelAction(string interactLabel) {
            _interactLabel = interactLabel;
        }
        
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (interactable is Location location) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Low, VibrationDuration.VeryShort);
                var portal = location.Element<Portal>();
                portal.Execute(Hero.Current);
            }
        }
    }
}