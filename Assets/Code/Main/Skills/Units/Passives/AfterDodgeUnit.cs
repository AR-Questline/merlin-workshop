using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Awaken.TG.VisualScripts.Units.Listeners.Events;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnityEngine.Scripting.Preserve]
    public class AfterDodgeUnit : EvtHero<Hero, bool> {
        protected override Event<Hero, bool> Event => Hero.Events.AfterHeroDashed;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }
}