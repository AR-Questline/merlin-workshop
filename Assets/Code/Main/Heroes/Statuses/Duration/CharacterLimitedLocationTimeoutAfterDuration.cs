using Awaken.TG.Main.AI.Utils;
using Awaken.TG.MVC;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class CharacterLimitedLocationTimeoutAfterDuration: DurationProxy<ICharacterLimitedLocation>, IWithDuration  {
        public override ushort TypeForSerialization => SavedModels.CharacterLimitedLocationTimeoutAfterDuration;

        public override IModel TimeModel => this;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] CharacterLimitedLocationTimeoutAfterDuration() { }
        public CharacterLimitedLocationTimeoutAfterDuration(IDuration duration) : base(duration) { }
        
        protected override void OnDurationElapsed() {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }
            
            ParentModel.Destroy();
        }
    }
}