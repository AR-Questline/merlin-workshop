using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class SkillOwnerContext : IListenerContext {
        public IModel Model => Character;
        public Location Location => Character is NpcElement npc ? npc.ParentModel : null;
        public ICharacter Character => Skill.Owner;
        public IAlive Alive => Character;
        public Skill Skill { get; }
        public Item Item => null;
        public Status Status => null;

        public SkillOwnerContext(Skill skill) {
            Skill = skill;
        }
    }
    
    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnitTitle("SkillOwnerContext")]
    [UnityEngine.Scripting.Preserve]
    public class SkillOwnerContextUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("context", flow => new SkillOwnerContext(this.Skill(flow)));
        }
    }
}