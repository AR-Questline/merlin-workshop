using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Events {
    public abstract class BaseEventUnit : EventUnit<EmptyEventArgs> {
        protected abstract string HookValue { get; }
        protected override bool register => true;
        public override EventHook GetHook(GraphReference reference) => new(HookValue, reference.gameObject);
    }
    
    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("On Enter Combat")]
    [UnityEngine.Scripting.Preserve]
    public class EnterCombatUnit : BaseEventUnit {
        public const string Hook = "CombatEntered";
        protected override string HookValue => Hook;
    }
    
    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("On Exit Combat")]
    [UnityEngine.Scripting.Preserve]
    public class ExitCombatUnit : BaseEventUnit {
        public const string Hook = "CombatExited";
        protected override string HookValue => Hook;
    }
    
    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("On Death")]
    public class DeathUnit : BaseEventUnit {
        public const string Hook = "CharacterDied";
        protected override string HookValue => Hook;
    }

    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("On Enter Idle")]
    [UnityEngine.Scripting.Preserve]
    public class EnterIdleUnit : BaseEventUnit {
        public const string Hook = "IdleEntered";
        protected override string HookValue => Hook;
    }
    
    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("On Exit Idle")]
    [UnityEngine.Scripting.Preserve]
    public class ExitIdleUnit : BaseEventUnit {
        public const string Hook = "IdleExited";
        protected override string HookValue => Hook;
    }
    
    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("On Visual Loaded")]
    public class VisualLoadedUnit : BaseEventUnit {
        public const string Hook = "VisualLoaded";
        protected override string HookValue => Hook;
    }
    
    
    [UnitCategory("AR/AI_Systems/Events")]
    [UnitTitle("After Location Initialized")]
    public class AfterLocationInitializedUnit : BaseEventUnit {
        public const string Hook = "AfterLocationInitialized";
        protected override string HookValue => Hook;
    }
}