using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class InteractionTriggerSkills : AbstractLocationAction, ISkillOwner, IRefreshedByAttachment<InteractionTriggerSkillsAttachment> {
        public override ushort TypeForSerialization => SavedModels.InteractionTriggerSkills;
        
        // === Fields & Properties
        InteractionTriggerSkillsAttachment _spec;
        WeakModelRef<ICharacter> _currentlyInteractingCharacter;
        bool _locked;
        
        public ICharacter Character => _currentlyInteractingCharacter.Get();
        
        public void InitFromAttachment(InteractionTriggerSkillsAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(InitSkills);
        }

        void InitSkills() {
            foreach (var skill in _spec.Skills.Select(s => s.CreateSkill())) {
                AddElement(skill);
                skill.MarkedNotSaved = true;
            }
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            _currentlyInteractingCharacter = Hero.Current;

            foreach (var skill in Elements<Skill>()) {
                skill.Submit();
            }

            foreach (var skill in Elements<Skill>()) {
                skill.Cancel();
            }

            if (_spec.setAnimatorParametersOnTrigger && ParentModel.TryGetElement<AnimatorElement>(out var animatorElement)) {
                _spec.ApplyAnimatorParameters(animatorElement);
            }

            ResetInteractingCharacter().Forget();
        }

        async UniTaskVoid ResetInteractingCharacter() {
            bool anySkillOnCooldown;
            do {
                anySkillOnCooldown = false;
                foreach (var skill in Elements<Skill>()) {
                    if (skill.IsCoolingDown) {
                        anySkillOnCooldown = true;
                        break;
                    }
                }

                await UniTask.WaitForEndOfFrame();
            } while (!HasBeenDiscarded && anySkillOnCooldown);
            _currentlyInteractingCharacter = null;
        }

        public void LockForHero() {
            _locked = true;
        }

        public void UnlockForHero() {
            _locked = false;
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (_locked) {
                return ActionAvailability.Disabled;
            }
            
            foreach (var skill in Elements<Skill>()) {
                if (skill.IsCoolingDown) {
                    return ActionAvailability.Disabled;
                }
            }

            return Character != null ? ActionAvailability.Disabled : base.GetAvailability(hero, interactable);
        }
    }
}