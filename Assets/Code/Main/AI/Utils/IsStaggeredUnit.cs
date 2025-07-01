using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.AI.Utils {
    [UnitCategory("AR/AI_Systems/Utils")]
    [TypeIcon(typeof(Variables))]
    [UnityEngine.Scripting.Preserve]
    public class IsStaggeredUnit : ARUnit {
        protected override void Definition() {
            var npc = FallbackARValueInput("npc", NpcElement);
            ValueOutput("IsStaggered", flow => {
                NpcElement character = npc.Value(flow);
                return character?.ParentModel?.TryGetElement<EnemyBaseClass>()?.CurrentBehaviour.Get() is StaggerBehaviour;
            });
        }
    }
}