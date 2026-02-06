using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredRealTimeCondition : DeferredCondition {
        public override ushort TypeForSerialization => SavedTypes.DeferredRealTimeCondition;

        [Saved] ARTimeSpan _targetTime;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredRealTimeCondition() {}
        
        public DeferredRealTimeCondition(ARTimeSpan time) {
            _targetTime = time;
        }

        public override bool Fulfilled() {
            if (Time.time == 0) {
                return false;
            }
            
            return World.Only<GameRealTime>().PlayRealTime.TotalSeconds >= _targetTime.TotalSeconds;
        }
        
        public override bool Equals(System.Object other) {
            if (other is not DeferredRealTimeCondition otherCondition) {
                return false;
            }
            if (!_targetTime.Equals(otherCondition._targetTime)) {
                return false;
            }
            return true;
        }
    }
}