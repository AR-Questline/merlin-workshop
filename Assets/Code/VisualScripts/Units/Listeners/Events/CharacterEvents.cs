using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [UnitCategory("AR/General/Events/Character")]
    public abstract class EvtCharacter<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, ICharacter { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtCharacterLimitedLocationsChanged : EvtCharacter<ICharacter, int> {
        protected override Event<ICharacter, int> Event => CharacterLimitedLocations.Events.CharacterLimitedLocationsChanged;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtSummonSpawned : EvtCharacter<ICharacter, INpcSummon> {
        protected override Event<ICharacter, INpcSummon> Event => INpcSummon.Events.SummonSpawned;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtCharacterBlockBegun : EvtCharacter<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnBlockBegun;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtCharacterBlockEnded : EvtCharacter<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnBlockEnded;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtCharacterParryBegun : EvtCharacter<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnParryBegun;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
}