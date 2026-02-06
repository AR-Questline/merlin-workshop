using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    
    [UnitCategory("AR/General/Events/Hero")]
    public abstract class EvtHero<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtHeroEnteredPortal : EvtHero<Hero, Hero> {
        protected override Event<Hero, Hero> Event => Hero.Events.WalkedThroughPortal;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtHeroRested : EvtHero<Hero, int> {
        protected override Event<Hero, int> Event => Hero.Events.AfterHeroRested;
        protected override Hero Source(IListenerContext context) => context.Character as Hero;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtHeroSummon : GraphEvent<IModel, Model, NpcElement> {
        protected override Event<IModel, Model> Event => World.Events.ModelAdded<NpcHeroSummon>();
        protected override IModel Source(IListenerContext context) => context.Model;
        protected override NpcElement Payload(Model payload) => payload is NpcHeroSummon summon ? summon.ParentModel : null;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtHeroCrouchToggled : GraphEvent<Hero, bool> {
        protected override Event<Hero, bool> Event => Hero.Events.HeroCrouchToggled;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtWyrdSkillToggled : GraphEvent<Hero, bool> {
        protected override Event<Hero, bool> Event => Hero.Events.WyrdskillToggled;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtItemCrafted : GraphEvent<Hero, CreatedEvent> {
        protected override Event<Hero, CreatedEvent> Event => Crafting.Events.Created;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtItemCraftedAndAddedToHeroItems : GraphEvent<Hero, CreatedEvent> {
        protected override Event<Hero, CreatedEvent> Event => Crafting.Events.CreatedAddedToHero;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtHeroPerspectiveChanged : GraphEvent<Hero, bool> {
        protected override Event<Hero, bool> Event => Hero.Events.HeroPerspectiveChanged;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class OnKnockDownEnterUnit : GraphEvent<Hero, bool> {
        protected override Event<Hero, bool> Event => HeroKnockdown.Events.KnockdownEntered;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class OnKnockDownExitUnit : GraphEvent<Hero, bool> {
        protected override Event<Hero, bool> Event => HeroKnockdown.Events.KnockdownExited;
        protected override Hero Source(IListenerContext context) => context.Model as Hero;
    }
}