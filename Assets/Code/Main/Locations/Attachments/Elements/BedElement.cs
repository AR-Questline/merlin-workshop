using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class BedElement : AbstractLocationAction, IRefreshedByAttachment<BedAttachment> {
        public override ushort TypeForSerialization => SavedModels.BedElement;

        BedAttachment _spec;
        string _interactLabel;
        
        public override InfoFrame ActionFrame => !string.IsNullOrWhiteSpace(_interactLabel) ? 
            new InfoFrame(_interactLabel, HeroHasRequiredItem()) : 
            base.ActionFrame;
        
        public void InitFromAttachment(BedAttachment spec, bool isRestored) {
            _spec = spec;
            _interactLabel = spec.label.ToString();
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return hero.IsInCombat() ? ActionAvailability.Disabled : base.GetAvailability(hero, interactable);
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            var restPopupUI = new RestPopupUI(null, true);
            World.Add(restPopupUI);
            restPopupUI.ListenTo(RestPopupUI.Events.RestingStarted, OnRest, this);
            restPopupUI.ListenTo(Events.AfterDiscarded, _ => FinishInteraction(hero, interactable), this);
        }

        void OnRest(RestPopupUI _) {
            Hero.Current.ListenToLimited(Hero.Events.AfterHeroRested, OnHeroRested, this);
        }
        
        void OnHeroRested(int _) {
            var hero = Hero.Current;
            var statuses = hero.Statuses;
            statuses.RemoveAllStatusesOfType(StatusType.Debuff);
            
            if (_spec.statusToAdd.TryGet(out StatusTemplate statusToAdd)) {
                var source = StatusSourceInfo.FromStatus(statusToAdd);
                var duration = new TimeDuration(_spec.statusToAddDuration);
                statuses.AddStatus(statusToAdd, source, duration);
            }
            
            if (_spec.statusToRemove.TryGet(out StatusTemplate statusToRemove)) {
                statuses.RemoveAllStatus(statusToRemove);
            }

            if (StoryFlags.Get("SleepGrantRejuvenation")) {
                if (_spec.statusToAddOnRejuvenation.TryGet(out StatusTemplate statusToAddOnRejuvenation)) {
                    var source = StatusSourceInfo.FromStatus(statusToAddOnRejuvenation);
                    var duration = new TimeDuration(_spec.statusToAddOnRejuvenationDuration);
                    statuses.AddStatus(statusToAddOnRejuvenation, source, duration);
                }
            }

            hero.Health.SetToFull();
            hero.Mana.SetToFull();
            hero.Stamina.SetToFull();
        }
    }
}