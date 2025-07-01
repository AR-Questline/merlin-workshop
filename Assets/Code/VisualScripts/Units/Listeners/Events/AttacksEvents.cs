using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    
    [UnitCategory("AR/General/Events/Attack")]
    public abstract class EvtAttack<TSource, TPayload> : GraphEvent<TSource, TPayload> where TSource : class, IModel { }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnWeaponVisibilityToggled : EvtAttack<IItemOwner, bool> {
        protected override Event<IItemOwner, bool> Event => CharacterHandBase.Events.WeaponVisibilityToggled;
        protected override IItemOwner Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnAttackStart : EvtAttack<ICharacter, AttackParameters> {
        protected override Event<ICharacter, AttackParameters> Event => ICharacter.Events.OnAttackStart;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnAttackEnd : EvtAttack<ICharacter, AttackParameters> {
        protected override Event<ICharacter, AttackParameters> Event => ICharacter.Events.OnAttackEnd;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnSuccessfulAttackEnd : EvtAttack<ICharacter, AttackParameters> {
        protected override Event<ICharacter, AttackParameters> Event => ICharacter.Events.OnSuccessfulAttackEnd;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }

    [UnityEngine.Scripting.Preserve]
    public class EvtOnFailedAttackEnd : EvtAttack<ICharacter, AttackParameters> {
        protected override Event<ICharacter, AttackParameters> Event => ICharacter.Events.OnFailedAttackEnd;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnRangedWeaponFullyDrawn : EvtAttack<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnRangedWeaponFullyDrawn;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    class EvtOnFiredProjectile : EvtAttack<ICharacter, DamageDealingProjectile> {
        protected override Event<ICharacter, DamageDealingProjectile> Event => ICharacter.Events.OnFiredProjectile;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnBowZoomStart : EvtAttack<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnBowZoomStart;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnBowZoomEnd : EvtAttack<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnBowZoomEnd;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnBowDrawStart : EvtAttack<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnBowDrawStart;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnBowDrawEnd : EvtAttack<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnBowDrawEnd;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
    
    [UnityEngine.Scripting.Preserve]
    public class EvtOnHeavyAttackHoldStarted : EvtAttack<ICharacter, ICharacter> {
        protected override Event<ICharacter, ICharacter> Event => ICharacter.Events.OnHeavyAttackHoldStarted;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
        
    [UnityEngine.Scripting.Preserve]
    public class EvtOnInvoked : EvtAttack<ICharacter, Item> {
        protected override Event<ICharacter, Item> Event => ICharacter.Events.OnEffectInvoked;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
            
    [UnityEngine.Scripting.Preserve]
    public class EvtOnMagicGauntletHit : EvtAttack<ICharacter, MagicGauntletData> {
        protected override Event<ICharacter, MagicGauntletData> Event => ICharacter.Events.OnMagicGauntletHit;
        protected override ICharacter Source(IListenerContext context) => context.Character;
    }
}
