using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;
using FMODUnity;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class InteractAction : AbstractLocationAction, IRefreshedByAttachment<InteractAttachment> {
        public override ushort TypeForSerialization => SavedModels.InteractAction;

        string _interactLabel;
        EventReference _interactionSound;
        bool _blockInCombat;
        bool _waitForManualFinishAction;

        public override InfoFrame ActionFrame => !string.IsNullOrWhiteSpace(_interactLabel) ? 
            new InfoFrame(_interactLabel, HeroHasRequiredItem()) : 
            base.ActionFrame;

        public void InitFromAttachment(InteractAttachment spec, bool isRestored) {
            _interactLabel = spec.customInteractLabel.ToString();
            _interactionSound = spec.interactionSound;
            _blockInCombat = spec.blockInCombat;
            _waitForManualFinishAction = spec.waitForManualFinishAction;
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (!_interactionSound.IsNull) {
                FMODManager.PlayOneShot(_interactionSound, interactable.InteractionPosition);
            }
            
            if (!_waitForManualFinishAction) {
                FinishInteraction(hero, interactable);
            }
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return _blockInCombat && hero.IsInCombat() 
                ? ActionAvailability.Disabled 
                : base.GetAvailability(hero, interactable);
        }
    }
}