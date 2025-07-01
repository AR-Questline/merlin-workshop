using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.Interactions {
    [SpawnsView(typeof(VHeroInteractionUI))]
    public partial class HeroInteractionUI : Element<Hero>, IUIPlayerInput, IHeroInteractionUI, IUniqueKeyProvider {
        public sealed override bool IsNotSaved => true;
        public bool Visible => true;
        public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.Gameplay.Interact.Yield();
        public virtual KeyIcon.Data UniqueKey => new(KeyBindings.Gameplay.Interact, false);
        public int InputPriority => 1;
        public IInteractableWithHero Interactable { get; }

        protected bool HasAction => _usedAction is { IsValidAction: true };
        protected IHeroAction _usedAction;

        public HeroInteractionUI(IInteractableWithHero interactable) {
            Interactable = interactable;
        }
        
        protected override void OnInitialize() {
            if (Interactable is IModel model) {
                model.ListenTo(Events.AfterChanged, TriggerChange, this);
            }
            World.Only<PlayerInput>().RegisterPlayerInput(this, this);
            ParentModel.ListenTo(ICharacter.Events.CombatEntered, OnCombatStateChanged, this);
            ParentModel.ListenTo(ICharacter.Events.CombatExited, OnCombatStateChanged, this);
        }

        static void OnCombatStateChanged() {
            World.Any<HeroInteractionUI>()?.TriggerChange();
        }

        public virtual UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction && HeroInteraction.StartInteraction(ParentModel, Interactable, out var newAction)) {
                // End previous one if for some reason it hasn't been ended
                EndAction();
                _usedAction = newAction;
                RewiredHelper.VibrateHighFreq(VibrationStrength.Low, VibrationDuration.VeryShort);
                return UIResult.Accept;
            } else if (HasAction && evt is UIKeyUpAction) {
                EndAction();
                RewiredHelper.VibrateHighFreq(VibrationStrength.Low, VibrationDuration.VeryShort);
                return UIResult.Accept;
            }
            return UIResult.Ignore;
        }

        protected void EndAction() {
            if (HasAction) {
                _usedAction.EndInteraction(ParentModel, Interactable);
                _usedAction = null;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            EndAction();
        }
    }
}