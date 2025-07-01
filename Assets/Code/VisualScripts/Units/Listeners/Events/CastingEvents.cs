using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnCastingBegunUnit : GraphEvent<ICharacter, CastSpellData> {
        protected override Event<ICharacter, CastSpellData> Event => ICharacter.Events.CastingBegun;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
        
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnCastingCanceledUnit : GraphEvent<ICharacter, CastSpellData> {
        protected override Event<ICharacter, CastSpellData> Event => ICharacter.Events.CastingCanceled;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
        
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnCastingEndedUnit : GraphEvent<ICharacter, CastSpellData> {
        protected override Event<ICharacter, CastSpellData> Event => ICharacter.Events.CastingEnded;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class EvtBeforeLightCastStartedUnit : GraphEvent<Item, bool> {
        protected override Event<Item, bool> Event => MagicFSM.Events.BeforeLightCastStarted;
        protected override Item Source(IListenerContext context) => context.Item;
    }
    
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class EvtBeforeHeavyCastStartedUnit : GraphEvent<Item, bool> {
        protected override Event<Item, bool> Event => MagicFSM.Events.BeforeHeavyCastStarted;
        protected override Item Source(IListenerContext context) => context.Item;
    }
}