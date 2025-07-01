using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.NPCs {
    [UnitCategory("AR/NPCs")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetStatusBuildupUnit : ARUnit {
        protected override void Definition() {
            var character = InlineARValueInput<ICharacter>("character", null);
            var statusType = InlineARValueInput<BuildupStatusType>("statusType", null);
            var buildup = InlineARValueInput<StatusStatsValues.StatusBuildupThreshold>("buildup", StatusStatsValues.StatusBuildupThreshold.Normal);

            DefineSimpleAction(flow => SetStatusBuildup(character.Value(flow), statusType.Value(flow), buildup.Value(flow)));
        }

        void SetStatusBuildup(ICharacter character, BuildupStatusType type, StatusStatsValues.StatusBuildupThreshold newBuildup) {
            character.Stat(type.BuildupStatType)?.SetTo(StatusStatsValues.GetThreshold(newBuildup, character.Tier));
        }
    }
    
    [UnitCategory("AR/NPCs")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetStatusEffectModifierUnit : ARUnit {
        protected override void Definition() {
            var character = InlineARValueInput<ICharacter>("character", null);
            var statusType = InlineARValueInput<BuildupStatusType>("statusType", BuildupStatusType.Bleed);
            var effectModifier = InlineARValueInput<StatusStatsValues.StatusEffectModifier>("effectModifier", StatusStatsValues.StatusEffectModifier.Normal);
            
            DefineSimpleAction(flow => SetStatusEffectModifier(character.Value(flow), statusType.Value(flow), effectModifier.Value(flow)));
        }
        
        void SetStatusEffectModifier(ICharacter character, BuildupStatusType type, StatusStatsValues.StatusEffectModifier newModifier) {
            character.Stat(type.EffectModifierType)?.SetTo(StatusStatsValues.GetModifier(newModifier));
        }
    }
}