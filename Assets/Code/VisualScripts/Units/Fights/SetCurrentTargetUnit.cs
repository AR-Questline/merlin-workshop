using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetCurrentTargetUnit : ARUnit {
        protected override void Definition() {
            var character = FallbackARValueInput("character", flow => flow.stack.self);
            var target = RequiredARValueInput<Transform>("target");
            DefineSimpleAction(flow => {
                var characterModel = VGUtils.GetModel<NpcElement>(character.Value(flow));
                var targetObject = target.Value(flow);
                if (characterModel == null || targetObject == null) {
                    return;
                }
                characterModel.ForceAddCombatTarget(VGUtils.GetModel<ICharacter>(targetObject.gameObject), true);
            });
        }
    }
}
