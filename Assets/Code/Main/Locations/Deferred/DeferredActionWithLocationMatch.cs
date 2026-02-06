using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredActionWithLocationMatch : DeferredActionRequiringVisualLoaded {
        public override ushort TypeForSerialization => SavedTypes.DeferredActionWithLocationMatch;

        static readonly List<Location> ReusableLocations = new(4);
        
        [Saved] LocationReference.Match _match;
        [Saved] DeferredLocationExecution _execution;

        public LocationReference.Match Match => _match;
        public DeferredLocationExecution Execution => _execution;

        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredActionWithLocationMatch() {}

        public DeferredActionWithLocationMatch(LocationReference.Match match, DeferredLocationExecution execution) 
            : this(match, execution, new List<DeferredCondition>()) { }

        public DeferredActionWithLocationMatch(LocationReference.Match match, DeferredLocationExecution execution, IEnumerable<DeferredCondition> conditions) : base(conditions) {
            _match = match;
            _execution = execution;
        }
        
        public override DeferredSystem.Result TryExecute() {
            return TryExecute(_match, _execution, this);
        }

        public static DeferredSystem.Result TryExecute(LocationReference.Match match, DeferredLocationExecution execution, DeferredActionWithLocationMatch action)  {
            ReusableLocations.Clear();
            match.Collect(ReusableLocations);
            foreach (var loc in ReusableLocations) {
                if (RequireWait(execution, loc)) {
                    if (action != null) {
                        action.WaitForVisualLoaded(loc);
                    } else {
                        ReusableLocations.Clear();
                        return DeferredSystem.Result.Fail;
                    }
                }
            }
            
            if (action is { ListenersCount: > 0 }) {
                ReusableLocations.Clear();
                return DeferredSystem.Result.Ignore;
            }
            if (ReusableLocations.Count == 0) {
                return DeferredSystem.Result.Fail;
            }
            foreach (var location in ReusableLocations) {
                execution.Execute(location);
            }
            ReusableLocations.Clear();
            return DeferredSystem.Result.Success;
        }
        
        public override bool HasSimilarConditions(DeferredAction other) {
            if (other is not DeferredActionWithLocationMatch otherAction) {
                return false;
            }
            if (!_match.Equals(otherAction._match)) {
                return false;
            }
            return base.HasSimilarConditions(other);
        }
    }
}