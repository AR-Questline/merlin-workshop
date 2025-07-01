using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class SkillContext : IListenerContext {
        public IModel Model => Skill;
        public Location Location => null;
        public ICharacter Character => Skill.Owner;
        public IAlive Alive => Skill.Owner;
        public Skill Skill { get; }
        public Item Item => (Skill.ParentModel as ItemEffects)?.Item;
        public Status Status => Skill.ParentModel as Status;

        public SkillContext(Skill skill) {
            Skill = skill;
        }
        
        [UnitCategory("AR/General/Events/Context")]
        [TypeIcon(typeof(IListenerContext))]
        [UnitTitle("SkillContext")]
        [UnityEngine.Scripting.Preserve]
        public class SkillContextUnit : ARUnit {
            protected override void Definition() {
                var skill = RequiredARValueInput<Skill>("skill");
                ValueOutput("context", flow => new SkillContext(skill.Value(flow)));
            }
        }
    }
}