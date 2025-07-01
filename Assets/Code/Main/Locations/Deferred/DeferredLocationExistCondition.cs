using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredLocationExistCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredLocationExistCondition;

        [Saved] WeakModelRef<Location> _locationRef;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredLocationExistCondition() {}
        
        public DeferredLocationExistCondition(Location location) {
            _locationRef = location;
        }
        
        public override bool Fulfilled() {
            return _locationRef.Exists();
        }
    }
}