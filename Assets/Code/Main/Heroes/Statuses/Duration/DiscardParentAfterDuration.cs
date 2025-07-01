using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class DiscardParentAfterDuration : DurationProxy<Model>, IWithDuration {
        public override ushort TypeForSerialization => SavedModels.DiscardParentAfterDuration;

        public override IModel TimeModel => this;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] DiscardParentAfterDuration() { }
        public DiscardParentAfterDuration(IDuration duration) : base(duration) { }

        public new static class Events {
            public static readonly HookableEvent<DiscardParentAfterDuration, Model> DiscardingParent = new(nameof(DiscardingParent));
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel.HasBeenDiscarded || Events.DiscardingParent.RunHooks(this, ParentModel).Prevented) {
                return;
            }
            ParentModel.Discard();
        }
    }
}