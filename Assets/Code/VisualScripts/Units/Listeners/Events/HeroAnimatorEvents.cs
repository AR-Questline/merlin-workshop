using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/General/Events/HeroAnimator")]
    [UnityEngine.Scripting.Preserve]
    public abstract class EvtHeroAnimator<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtHeroDualWieldingStarted : EvtHero<Hero, Hero> {
        protected override Event<Hero, Hero> Event => DualHandedFSM.Events.DualWieldingStarted;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtHeroDualWieldingEnded : EvtHero<Hero, Hero> {
        protected override Event<Hero, Hero> Event => DualHandedFSM.Events.DualWieldingEnded;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtHeroSlid : EvtHero<Hero, bool> {
        protected override Event<Hero, bool> Event => Hero.Events.HeroSlid;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }
}