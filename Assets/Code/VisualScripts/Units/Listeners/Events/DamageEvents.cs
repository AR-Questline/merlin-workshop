using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    
    [UnitCategory("AR/General/Events/Damage")]
    public abstract class EvtDamage<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtBeforeDamageBlocked : EvtDamage<HealthElement, Damage> {
        protected override Event<HealthElement, Damage> Event => HealthElement.Events.BeforeDamageBlocked;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnDamageBlocked : EvtDamage<HealthElement, Damage> {
        protected override Event<HealthElement, Damage> Event => HealthElement.Events.OnDamageBlocked;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnBeforeDamageParried : EvtDamage<HealthElement, Damage> {
        protected override Event<HealthElement, Damage> Event => HealthElement.Events.BeforeDamageParried;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnDamageParried : EvtDamage<HealthElement, Damage> {
        protected override Event<HealthElement, Damage> Event => HealthElement.Events.OnDamageParried;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtBeforeDamageDealt : EvtDamage<ICharacter, Damage> {
        protected override Event<ICharacter, Damage> Event => HealthElement.Events.BeforeDamageDealt;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnDamageDealt : EvtDamage<ICharacter, DamageOutcome> {
        protected override Event<ICharacter, DamageOutcome> Event => HealthElement.Events.OnDamageDealt;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtBeforeDamageTaken : EvtDamage<HealthElement, Damage> {
        protected override Event<HealthElement, Damage> Event => HealthElement.Events.BeforeDamageTaken;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnDamageTaken : EvtDamage<HealthElement, DamageOutcome> {
        protected override Event<HealthElement, DamageOutcome> Event => HealthElement.Events.OnDamageTaken;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnDamageMultiplied : EvtDamage<ICharacter, ModifiedDamageInfo> {
        protected override Event<ICharacter, ModifiedDamageInfo> Event => HealthElement.Events.OnDamageMultiplied;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnDamageTakenMultiplied : EvtDamage<ICharacter, ModifiedDamageInfo> {
        protected override Event<ICharacter, ModifiedDamageInfo> Event => HealthElement.Events.OnDamageTakenMultiplied;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnitTitle("Evt Before Death")]
    [UnityEngine.Scripting.Preserve]
    public class EvtOnDeath : EvtDamage<IAlive, DamageOutcome> {
        protected override Event<IAlive, DamageOutcome> Event => IAlive.Events.BeforeDeath;
        protected override IAlive Source(IListenerContext context) => context.Alive;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtAfterDeath : EvtDamage<IAlive, DamageOutcome> {
        protected override Event<IAlive, DamageOutcome> Event => IAlive.Events.AfterDeath;
        protected override IAlive Source(IListenerContext context) => context.Alive;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnKill : EvtDamage<ICharacter, DamageOutcome> {
        protected override Event<ICharacter, DamageOutcome> Event => HealthElement.Events.OnKill;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnKillPrevented : EvtDamage<IAlive, KillPreventedData> {
        protected override Event<IAlive, KillPreventedData> Event => KillPreventionDispatcher.Events.KillPrevented;
        protected override IAlive Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnCombatEntered : EvtDamage<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.CombatEntered;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnCombatExited : EvtDamage<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.CombatExited;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class HookTakingDamage : GraphHookableEvent<HealthElement, Damage> {
        protected override HookableEvent<HealthElement, Damage> Event => HealthElement.Events.TakingDamage;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class HookBeforeTakenFinalDamage : GraphHookableEvent<HealthElement, Damage> {
        protected override HookableEvent<HealthElement, Damage> Event => HealthElement.Events.BeforeTakenFinalDamage;
        protected override HealthElement Source(IListenerContext context) => context.Alive?.HealthElement;
    }
}