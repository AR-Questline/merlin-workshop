using System.Linq;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RecallOwnedSummonsUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            DefineSimpleAction("Enter", "Exit", flow => {
                var item = this.Skill(flow).SourceItem;
                var ownedSummons = World.All<NpcHeroSummon>().Where(s => s.Item == item).ToArray();
                foreach (var summon in ownedSummons) {
                    summon.ParentModel.ParentModel.Kill(null, true);
                }
            });
        }
    }
}