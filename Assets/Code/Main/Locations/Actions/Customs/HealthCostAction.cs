using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions.Customs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public partial class HealthCostAction : AbstractLocationAction, IRefreshedByAttachment<HealthCostActionAttachment> {
        public override ushort TypeForSerialization => SavedModels.HealthCostAction;

        HealthCostActionAttachment _spec;
        string _interactLabel;
        float _costPerSecond;
        
        public override InfoFrame ActionFrame => !string.IsNullOrWhiteSpace(_interactLabel) ? 
            new InfoFrame(_interactLabel, HeroHasRequiredItem()) : 
            base.ActionFrame;

        public void InitFromAttachment(HealthCostActionAttachment spec, bool isRestored) {
            _spec = spec;
            _interactLabel = spec.CustomInteractLabel;
        }
        
        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (hero.Health <= 0f) return ActionAvailability.Disabled;
            return base.GetAvailability(hero, interactable);
        }

        public override IHeroInteractionUI InteractionUIToShow(IInteractableWithHero interactable) {
            _costPerSecond = _spec.TotalCost(Hero.Current.Health) / _spec.Duration;
            return new HeroInteractionHoldUI(interactable, _spec.Duration, _spec.UseHold, OnHoldAction);
        }

        void OnHoldAction(Hero hero) {
            if (hero.CharacterStats.IncomingDamage == 0) return;
            hero.Health.DecreaseBy(_costPerSecond * Time.deltaTime);
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (ParentModel.TryGetElement<LogicEmitterAction>(out var emitter)) {
                emitter.ChangeState();
                return;
            }
            Disabled = true;
            HeroInteraction.StartInteraction(hero, ParentModel, out _);
            Disabled = false;
        }
    }
}