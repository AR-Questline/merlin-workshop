using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredLocationMatchCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredLocationMatchCondition;

        [Saved] LocationReference.Match _match;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredLocationMatchCondition() { }
        
        public DeferredLocationMatchCondition(LocationReference.Match match) {
            _match = match;
        }
        
        public override bool Fulfilled() {
            var found = false;
            foreach (var location in _match.Find()) {
                found = true;
                if (!location.IsVisualLoaded) {
                    return false;
                }
            }
            return found;
        }
    }
}