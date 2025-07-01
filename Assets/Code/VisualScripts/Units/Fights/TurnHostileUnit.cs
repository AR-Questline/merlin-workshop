using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class TurnHostileUnit : ARUnit {
        protected override void Definition() {
            var me = RequiredARValueInput<ICharacter>("NPC to turn");
            var other = RequiredARValueInput<ICharacter>("Against this one");
            DefineSimpleAction(flow => {
                me.Value(flow).TurnHostileTo(AntagonismLayer.Story, other.Value(flow));
            });
        }
    }
}