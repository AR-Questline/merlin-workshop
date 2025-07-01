using System.Diagnostics;

namespace Awaken.TG.MVC.Events {
    /// <summary>
    /// Hookable events are used to allow listeners to modify or prevent an action being taken.
    /// They are named in present continuous. For example, a 'TakingDamage' event allows listeners
    /// to modify the amount of damage being taken or prevent it altogether.
    /// </summary>
    /// <typeparam name="TModel">the model type this event is connected to</typeparam>
    /// <typeparam name="TValue">the value/values that define the action and that the listeners modify</typeparam>
    [DebuggerDisplay("{Name} HookableEvent<{typeof(TModel).Name,nq}, {typeof(TValue).Name,nq}>")]
    public class HookableEvent<TModel, TValue> : Event<TModel, HookResult<TModel, TValue>> where TModel : IModel {
        // === Constructors

        public HookableEvent(string name) : base(name) { }

        // === Triggering

        /// <summary>
        /// Runs all the hooks tied to this event and returns the
        /// result. All listeners get a chance to alter the HookResult
        /// to either change the parameters of the action being hooked,
        /// or prevent it from happening entirely.
        /// </summary>
        public HookResult<TModel, TValue> RunHooks(TModel model, TValue value) {
            HookResult<TModel, TValue> result = new HookResult<TModel, TValue>(model, value);
            model.Trigger(this, result);
            return result;
        }
    }
}
