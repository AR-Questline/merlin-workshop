using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public partial class DeferredActionWithPresenceMatch : DeferredAction {
        public override ushort TypeForSerialization => SavedTypes.DeferredActionWithPresenceMatch;

        [Saved] PresenceData _presenceData;
        [Saved] DeferredLocationExecution _execution;
        static readonly List<Location> ReusableLocations = new();
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredActionWithPresenceMatch() {}
        
        public DeferredActionWithPresenceMatch(PresenceData presenceData, DeferredLocationExecution execution) : base(new List<DeferredCondition>(), presenceData.sceneRef) {
            _presenceData = presenceData;
            _execution = execution;
        }

        public override DeferredSystem.Result TryExecute() {
            return TryExecute(_presenceData, _execution);
        }
        
        public static DeferredSystem.Result TryExecute(PresenceData presenceData, DeferredLocationExecution execution) {
            ReusableLocations.Clear();
            foreach (var location in FindAllMatchingLocations(presenceData)) {
                if (!location.IsVisualLoaded) {
                    ReusableLocations.Clear();
                    return DeferredSystem.Result.RepeatNextFrame;
                }

                ReusableLocations.Add(location);
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

       static IEnumerable<Location> FindAllMatchingLocations(PresenceData presenceData) {
           foreach (var presence in World.All<NpcPresence>()) {
               if (presence != null && presence.RichLabelSet.EqualRichLabelGuids(presenceData.richLabelSet)) {
                   yield return presence.ParentModel;
               }
           }
        }
    }
}