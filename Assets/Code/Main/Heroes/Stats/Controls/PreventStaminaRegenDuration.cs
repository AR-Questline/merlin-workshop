using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    public partial class PreventStaminaRegenDuration : DurationProxy<ICharacter>, IPreventStaminaRegen {
        public sealed override bool IsNotSaved => true;

        public override IModel TimeModel => ParentModel;

        // === Constructor
        PreventStaminaRegenDuration(IDuration duration) : base(duration) { }

        // === Life Cycle
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            ParentModel.Trigger(IPreventStaminaRegen.Events.StaminaRegenBlocked, true);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Trigger(IPreventStaminaRegen.Events.StaminaRegenBlocked, false);
        }

        // === Public API
        public static void Prevent(ICharacter character, IDuration duration) {
            PreventStaminaRegenDuration prevent = character.TryGetElement<PreventStaminaRegenDuration>();
            if (prevent != null) {
                prevent.Duration.Renew(duration);
            } else {
                character.AddElement(new PreventStaminaRegenDuration(duration));
            }
        }
        
        public static void PreventWithStatus(ICharacter character, IDuration duration, IDuration depletedStatusDuration) {
            Prevent(character, duration);

            var statusTemplate = GameConstants.Get.StaminaDepletedStatusTemplate;
            var statusSourceInfo = StatusSourceInfo.FromStatus(statusTemplate).WithCharacter(character);
            character.Statuses.AddStatus(statusTemplate, statusSourceInfo, depletedStatusDuration);
        }
    }
}