using Awaken.Utility;
using System.Linq;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredAnyLocationExistCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredAnyLocationExistCondition;

        [Saved] LocationReference _locationRef;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredAnyLocationExistCondition() {}
        
        public DeferredAnyLocationExistCondition(LocationReference locationRef) {
            _locationRef = locationRef;
        }
        
        public override bool Fulfilled() {
            var locations = _locationRef.MatchingLocations(null);
            bool any = false;
            foreach (var location in locations) {
                any = true;  
                if (!location.IsVisualLoaded) {
                    return false;
                }
            }
            return any;
        }
    }
}