using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    public partial class PreventStaminaRegenDuration : DurationProxy<ICharacter>, IPreventStaminaRegen {
        public sealed override bool IsNotSaved => true;

        public override IModel TimeModel => ParentModel;
        public StaminaRegenBlockType BlockType { get; }

        // === Constructor
        PreventStaminaRegenDuration(StaminaRegenBlockType blockType, IDuration duration) : base(duration) {
            BlockType = blockType;
        }

        // === Life Cycle
        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            ParentModel.Trigger(IPreventStaminaRegen.Events.StaminaRegenBlocked, 
                new StaminaRegenBlockParams(true, BlockType));
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Trigger(IPreventStaminaRegen.Events.StaminaRegenBlocked, 
                new StaminaRegenBlockParams(false, BlockType));
        }

        // === Public API
        public static void Prevent(ICharacter character, StaminaRegenBlockType blockType, IDuration duration) {
            var preventElements = character.Elements<PreventStaminaRegenDuration>();
            foreach (var prevent in preventElements) {
                if (prevent.BlockType == blockType) {
                    prevent.Duration.Renew(duration);
                    return;
                }
            }
            character.AddElement(new PreventStaminaRegenDuration(blockType, duration));
        }
        
        public static void PreventWithStatus(ICharacter character, StaminaRegenBlockType blockType, 
            IDuration duration, IDuration depletedStatusDuration) {
            Prevent(character, blockType, duration);

            var statusTemplate = GameConstants.Get.StaminaDepletedStatusTemplate;
            var statusSourceInfo = StatusSourceInfo.FromStatus(statusTemplate).WithCharacter(character);
            character.Statuses.AddStatus(statusTemplate, statusSourceInfo, depletedStatusDuration);
        }
    }
}