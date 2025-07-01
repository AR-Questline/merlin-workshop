using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredTimeCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredTimeCondition;

        [Saved] ARDateTime _targetTime;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredTimeCondition() {}
        
        public DeferredTimeCondition(ARDateTime time) {
            _targetTime = time;
        }

        public override bool Fulfilled() {
            return World.Only<GameRealTime>().WeatherTime >= _targetTime;
        }
    }
}