using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class HeroContext : IListenerContext {
        public IModel Model => Character;
        public Location Location => null;
        public ICharacter Character => Hero.Current;
        public IAlive Alive => Hero.Current;
        public Skill Skill => null;
        public Item Item => null;
        public Status Status => null;
    }
    
    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnitTitle("HeroContext")]
    [UnityEngine.Scripting.Preserve]
    public class HeroContextUnit : Unit {
        protected override void Definition() {
            ValueOutput("context", _ => new HeroContext());
        }
    }
}