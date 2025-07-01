using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.NPCs {
    [UnitCategory("AR/NPCs")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class NPCPerformInteractionUnit : ARUnit {
        protected override void Definition() {
            ARValueInput<string> idInput = InlineARValueInput("uniqueId", string.Empty);
            ARValueInput<NpcElement> characterInput = RequiredARValueInput<NpcElement>("character");

            DefineSimpleAction(flow => {
                NpcElement npc = characterInput.Value(flow);
                string id = idInput.Value(flow);
                if (npc == null || string.IsNullOrEmpty(id)) return;
                npc.Behaviours.AddOverride(new InteractionUniqueFinder(id), null);
            });
        }
    }
}