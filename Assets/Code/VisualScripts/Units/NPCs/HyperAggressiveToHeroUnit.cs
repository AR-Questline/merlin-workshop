using Awaken.TG.Main.AI;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.NPCs {
    [UnitCategory("AR/NPCs")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class HyperAggressiveToHeroUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            var statusOwner = RequiredARValueInput<Status>("statusOwner");
            var priority = InlineARValueInput("priority", 1);
            
            DefineNoNameAction(flow => {
                var npc = character.Value(flow);
                var status = statusOwner.Value(flow);
                
                if (npc == null || status == null) return;
                HyperAggressiveToHero.Add(npc, priority.Value(flow), status);
            });
        }
    }
    
    [UnitCategory("AR/NPCs")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveHyperAggressiveToHeroUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            var statusOwner = RequiredARValueInput<Status>("statusOwner");
            
            DefineNoNameAction(flow => {
                var npc = character.Value(flow);
                var status = statusOwner.Value(flow);
                if (npc == null || status == null) return;
                
                foreach (var aggression in npc.Elements<HyperAggressiveToHero>().Reverse()) {
                    if (aggression.OwnedBy(status)) {
                        aggression.Discard();
                    }
                }
            });
        }
    }
}