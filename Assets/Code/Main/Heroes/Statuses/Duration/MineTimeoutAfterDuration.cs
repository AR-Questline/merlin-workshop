using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.MVC;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class MineTimeoutAfterDuration : DurationProxy<PlacedMine>, IWithDuration  {
        public override ushort TypeForSerialization => SavedModels.MineTimeoutAfterDuration;

        public override IModel TimeModel => this;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] MineTimeoutAfterDuration() { }
        public MineTimeoutAfterDuration(IDuration duration) : base(duration) { }
        
        protected override void OnDurationElapsed() {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }
            
            ParentModel.Destroy();
        }
    }
}