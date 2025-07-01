namespace Awaken.TG.MVC.Events {
    /// <summary>
    /// Used as payload by hookable events that allow listeners to modify an action being taken.
    /// For example, listener hooked into the 'TakingDamage' event can modify
    /// the amount of damage dealt via the hook, or prevent it completely.
    /// </summary>
    public class HookResult<M, V> : PreventableHook where M : IModel {
        // === Data

        public M Model { get; }
        public V Value { get; set; }
        public bool Prevented { get; private set; } = false;

        // === Constructors

        public HookResult(M model, V value) {
            Model = model;
            Value = value;
        }

        // === Operation

        public override void Prevent() {
            Prevented = true;
        }
    }

    /// <summary>
    /// For visual scripting access
    /// </summary>
    public class PreventableHook {
        public virtual void Prevent() { }
    }
}
