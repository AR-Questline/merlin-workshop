using System.Collections.Generic;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredLocationMatchCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredLocationMatchCondition;

        static readonly List<Location> ReusableLocations = new(4);

        [Saved] LocationReference.Match _match;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredLocationMatchCondition() { }
        
        public DeferredLocationMatchCondition(LocationReference.Match match) {
            _match = match;
        }
        
        public override bool Fulfilled() {
            ReusableLocations.Clear();
            _match.Collect(ReusableLocations);
            var found = false;
            foreach (var location in ReusableLocations) {
                found = true;
                if (!location.IsVisualLoaded) {
                    ReusableLocations.Clear();
                    return false;
                }
            }
            ReusableLocations.Clear();
            return found;
        }
        
        public override bool Equals(System.Object other) {
            if (other is not DeferredLocationMatchCondition otherCondition) {
                return false;
            }
            if (!_match.Equals(otherCondition._match)) {
                return false;
            }
            return true;
        }
    }
}