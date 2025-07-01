using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class BarkUnit : ARUnit {
        protected override void Definition() {
            var npcInput = FallbackARValueInput<NpcElement>("npc", _ => null);
            var barkIdInput = InlineARValueInput("barkId", string.Empty);
            var isImportantInput = InlineARValueInput("important", false);
            var rangeInput = InlineARValueInput("range", BarkRange.Short);
            var checkCooldownInput = InlineARValueInput("checkCooldown", false);

            DefineSimpleAction(flow => {
                NpcElement npc = npcInput.Value(flow);
                string barkId = barkIdInput.Value(flow);
                float range = rangeInput.Value(flow).ToFloat();
                bool isImportant = isImportantInput.Value(flow);
                bool checkCooldown = checkCooldownInput.Value(flow);
                
                var bark = npc?.ParentModel?.TryGetElement<BarkElement>();
                if (npc == null || bark == null) {
                    Log.Important?.Error($"This location: {npc?.ID} doesn't have BarkElement!");
                    return;
                }

                BarkElement.BarkType barkType = isImportant ? BarkElement.BarkType.Important : BarkElement.BarkType.NotImportant;
                bark.TryBark(barkId, barkType, range, checkCooldown);
            });
        }
    }
}