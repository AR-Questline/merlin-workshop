using System.Linq;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillNearbyDeadBodiesCondition : ARUnit, ISkillUnit {
        protected override void Definition() {
            var range = FallbackARValueInput("range", _ => 5f);
            ValueOutput("anyDeadBodyNearBy",
                flow => {
                    Vector3 coords = this.Skill(flow).Owner.Coords;
                    return World.Services.TryGet<NpcGrid>()?.GetNpcDummiesInSphere(coords, range.Value(flow)).Any() ?? false;
                });
        }
    }
}