using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class TurnFriendlyUnit : ARUnit {
        protected override void Definition() {
            var inMe = RequiredARValueInput<ICharacter>("NPC to turn");
            var inOther = RequiredARValueInput<ICharacter>("Against this one");

            DefineSimpleAction(flow => {
                var me = inMe.Value(flow);
                var other = inOther.Value(flow);
                
                if (me is not Main.Fights.NPCs.NpcElement npc) {
                    return;
                }
                me.TurnFriendlyTo(AntagonismLayer.Story, other);
                npc.RemoveCombatTarget(other);
            });
        }
    }
}