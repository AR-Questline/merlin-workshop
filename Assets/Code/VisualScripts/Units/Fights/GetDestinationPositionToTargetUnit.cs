using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetDestinationPositionToTargetUnit : ARUnit {
        protected override void Definition() {
            var myPosition = FallbackARValueInput("myPosition", flow => flow.stack.self.gameObject.transform.position);
            var character = FallbackARValueInput("character", flow => flow.stack.self);
            ValueOutput("destinationPosition", flow => {
                var targetTransform = VGUtils.GetModel<NpcElement>(character.Value(flow)).GetCurrentTarget()?.CharacterView?.transform;
                if (targetTransform == null) {
                    return Vector3.zero;
                }
                Vector3 targetPosition = targetTransform.position;
                Vector3 ourPosition = myPosition.Value(flow);
                Vector3 dirFromTarget = ourPosition - targetPosition;
                return targetPosition + dirFromTarget.normalized * VHeroCombatSlots.CombatSlotOffset;
            });
        }
    }
}