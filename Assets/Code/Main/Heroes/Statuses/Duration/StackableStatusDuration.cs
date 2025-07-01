using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class StackableStatusDuration : StatusDuration {
        public override ushort TypeForSerialization => SavedModels.StackableStatusDuration;

        public new static StackableStatusDuration Create(Status status, IDuration duration) {
            duration = ApplyBuffDurationMultiplier(status, duration);
            return new StackableStatusDuration(duration);
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] StackableStatusDuration() { }
        protected StackableStatusDuration(IDuration duration) : base(duration) { }
        
        protected override void OnFullyInitialized() {
            Duration.ListenTo(IDuration.Events.Elapsed, Callback, this);
        }

        void Callback(HookResult<IDuration, bool> hook) {
            if (HasBeenDiscarded) {
                return;
            }
            if (ParentModel.StackLevel > 1) {
                hook.Prevent();
                hook.Model.ResetDuration();
                ParentModel.ConsumeStack();
                return;
            }
            Discard();
        }
    }
}