using Awaken.TG.Main.AI;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class EnterCombatUnit : ARUnit {
        protected override void Definition() {
            var characterInput = FallbackARValueInput<IModel>("character", VGUtils.My<NpcElement>);
            var targetInput = RequiredARValueInput<IModel>("target");
            DefineNoNameAction(flow => {
                var character = characterInput.ModelValue<NpcAI>(flow);
                var target = targetInput.ModelValue<ICharacter>(flow);
                if (character != null && target != null) {
                    character.EnterCombatWith(target);
                } else {
                    Log.Important?.Error($"Failed to invoke EnterCombatUnit in: {flow.stack.gameObject.name}");
                }
            });
        }
    }
}