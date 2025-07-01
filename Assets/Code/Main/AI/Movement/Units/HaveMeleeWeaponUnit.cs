using System.Linq;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.AI.Movement.Units {
    [UnitCategory("AR/AI_Systems/Movement/Availability")]
    [UnityEngine.Scripting.Preserve]
    public class HaveMeleeWeaponUnit : ARUnit {
        protected override void Definition() {
            var npc = FallbackARValueInput("npc", NpcElement);
            ValueOutput("HaveMeleeWeapon", flow => {
                Main.Fights.NPCs.NpcElement npcElement = npc.Value(flow);
                return npcElement.Inventory.Items.Any(i => i.IsWeapon && !i.IsFists);
            });
        }
    }
}
