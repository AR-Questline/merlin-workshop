using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetCharacter : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public CharacterType type;

        protected override void Definition() {
            if (type == CharacterType.Hero) {
                ValueOutput("character", _ => Hero.Current);
            } else if (type == CharacterType.Npc) {
                var location = RequiredARValueInput<Location>("location");
                ValueOutput("character", flow => location.Value(flow).Element<NpcElement>());
            }
        }
        
        public enum CharacterType {
            Hero,
            Npc
        }
    }
}