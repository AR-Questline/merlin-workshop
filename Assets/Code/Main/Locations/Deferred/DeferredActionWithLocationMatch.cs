using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredActionWithLocationMatch : DeferredAction {
        public override ushort TypeForSerialization => SavedTypes.DeferredActionWithLocationMatch;

        static readonly List<Location> ReusableLocations = new();
        
        [Saved] LocationReference.Match _match;
        [Saved] DeferredLocationExecution _execution;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredActionWithLocationMatch() {}

        public DeferredActionWithLocationMatch(LocationReference.Match match, DeferredLocationExecution execution) 
            : this(match, execution, new List<DeferredCondition>()) { }

        public DeferredActionWithLocationMatch(LocationReference.Match match, DeferredLocationExecution execution, IEnumerable<DeferredCondition> conditions) : base(conditions) {
            _match = match;
            _execution = execution;
        }
        
        public override DeferredSystem.Result TryExecute() {
            return TryExecute(_match, _execution);
        }

        public static DeferredSystem.Result TryExecute(LocationReference.Match match, DeferredLocationExecution execution) {
            ReusableLocations.Clear();
            foreach (var loc in match.Find()) {
                if (!loc.IsVisualLoaded) {
                    ReusableLocations.Clear();
                    return DeferredSystem.Result.RepeatNextFrame;
                }
                ReusableLocations.Add(loc);
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
    }
}