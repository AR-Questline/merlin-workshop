using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class CharacterContext : IListenerContext {
        public IModel Model => Character;
        public Location Location => Character is NpcElement npc ? npc.ParentModel : null;
        public ICharacter Character { get; }
        public IAlive Alive => Character;
        public Skill Skill => null;
        public Item Item => null;
        public Status Status => null;

        public CharacterContext(ICharacter character) {
            Character = character;
        }
    }
    
    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnitTitle("CharacterContext")]
    [UnityEngine.Scripting.Preserve]
    public class CharacterContextUnit : ARUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            ValueOutput("context", flow => new CharacterContext(character.Value(flow)));
        }
    }
}