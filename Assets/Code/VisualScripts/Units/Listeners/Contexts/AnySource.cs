using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class AnySource : IListenerContext {
        public IModel Model => null;
        public Location Location => null;
        public ICharacter Character => null;
        public IAlive Alive => null;
        public Skill Skill => null;
        public Item Item => null;
        public Status Status => null;
    }

    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnityEngine.Scripting.Preserve]
    public class AnyContext : Unit {
        protected override void Definition() {
            ValueOutput("context", _ => new AnySource());
        }
    }
}