using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class TravelAction : AbstractHeroAction<Portal> {
        public sealed override bool IsNotSaved => true;

        readonly string _interactLabel;
        
        // Info frame bool decides if the interaction is enabled
        public override InfoFrame ActionFrame => Hero.Current.IsInCombat() 
                                                     ? new InfoFrame(LocTerms.PortalBlockedByCombat.Translate(), false) 
                                                     : !string.IsNullOrWhiteSpace(_interactLabel) 
                                                         ? new InfoFrame(_interactLabel, HeroHasRequiredItem()) 
                                                         : base.ActionFrame;

        public TravelAction(string interactLabel) {
            _interactLabel = interactLabel;
        }
        
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (Hero.Current.IsInCombat()) {
                return;
            }
            if (interactable is Location location) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Low, VibrationDuration.VeryShort);
                var portal = location.Element<Portal>();
                portal.Execute(hero);
            }
        }
    }
}