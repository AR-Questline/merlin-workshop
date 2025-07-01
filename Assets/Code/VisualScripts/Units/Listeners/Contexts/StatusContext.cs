using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class StatusContext : IListenerContext {
        public IModel Model => Status;
        public Location Location => null;
        public ICharacter Character => Status.Character;
        public IAlive Alive => Character;
        public Skill Skill => Status.Skill;
        public Item Item => null;
        public Status Status { get; }

        public StatusContext(Status status) {
            Status = status;
        }
    }
    
    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnitTitle("StatusContext")]
    [UnityEngine.Scripting.Preserve]
    public class StatusContextUnit : ARUnit {
        protected override void Definition() {
            var status = RequiredARValueInput<Status>("status");
            ValueOutput("context", flow => new StatusContext(status.Value(flow)));
        }
    }
}