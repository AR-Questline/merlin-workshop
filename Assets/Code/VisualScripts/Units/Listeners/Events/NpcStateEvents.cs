using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/General/Events/NpcStates")]
    public abstract class EvtNpcState<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }

    [UnityEngine.Scripting.Preserve]
    public class EvtEnterNpcState : EvtNpcState<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.CombatEntered;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtExitNpcState : EvtNpcState<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.CombatExited;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtLoseConscious : EvtNpcState<NpcElement, UnconsciousElement> {
        protected override Event<NpcElement, UnconsciousElement> Event => UnconsciousElement.Events.LoseConscious;
        protected override NpcElement Source(IListenerContext context) => context.Character as NpcElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtRegainConscious : EvtNpcState<NpcElement, UnconsciousElement> {
        protected override Event<NpcElement, UnconsciousElement> Event => UnconsciousElement.Events.RegainConscious;
        protected override NpcElement Source(IListenerContext context) => context.Character as NpcElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtStaggered : EvtNpcState<NpcElement, NpcElement> {
        protected override Event<NpcElement, NpcElement> Event => EnemyBaseClass.Events.Staggered;
        protected override NpcElement Source(IListenerContext context) => context.Character as NpcElement;
    }
}