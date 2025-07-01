using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class PerformHostileAction : ARUnit, ISkillUnit {
        protected override void Definition() {
            var attacker = FallbackARValueInput("attacker", flow => this.Skill(flow).Owner);
            var receiver = FallbackARValueInput("receiver", flow => this.Skill(flow).Owner);
            var item = FallbackARValueInput("item", flow => GetSkillItemUnit.GetSkillItem(flow, this));
            var damageSource = FallbackARValueInput("damageSource", flow => DamageType.PhysicalHitSource);

            DefineSimpleAction(flow => {
                if (receiver.Value(flow) is NpcElement npc) {
                    npc.NpcAI?.ReceiveHostileAction(attacker.Value(flow), item.Value(flow), damageSource.Value(flow));
                }
            });
        }
    }
}