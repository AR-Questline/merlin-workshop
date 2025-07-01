using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetSummonManaCostUnit : ARUnit {
        protected override void Definition() {
            var summon = RequiredARValueInput<NpcElement>("summon");
            var manaCost = ValueOutput<float>("manaCost");
            DefineSimpleAction(flow => flow.SetValue(manaCost, summon.Value(flow).Element<INpcSummon>().ManaExpended));
        }
    }
}