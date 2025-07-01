using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Discard Locations from Spawners"), NodeSupportsOdin]
    public class SEditorDiscardSpawnedLocations : EditorStep {
        public LocationReference locationReference;
        public bool discardNormalLocations = true;
        public bool discardWyrdspawnLocations = false;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDiscardSpawnedLocations {
                locationReference = locationReference,
                discardNormalLocations = discardNormalLocations,
                discardWyrdspawnLocations = discardWyrdspawnLocations
            };
        }
    }

    public partial class SDiscardSpawnedLocations : StoryStepWithLocationRequirement {
        public LocationReference locationReference;
        public bool discardNormalLocations = true;
        public bool discardWyrdspawnLocations = false;
        
        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(discardNormalLocations, discardWyrdspawnLocations);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_DiscardSpawnedLocation;

            [Saved] bool _discardNormalLocations;
            [Saved] bool _discardWyrdspawnLocations;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            
            public StepExecution(bool discardNormalLocations, bool discardWyrdspawnLocations) {
                _discardNormalLocations = discardNormalLocations;
                _discardWyrdspawnLocations = discardWyrdspawnLocations;
            }
            
            public override void Execute(Location location) {
                var spawner = location.TryGetElement<BaseLocationSpawner>();
                if (spawner == null) {
                    Log.Minor?.Error($"No spawner on location: {LogUtils.GetDebugName(location)}");
                    return;
                }
                if (_discardNormalLocations) {
                    spawner.DiscardAllSpawnedLocations();
                }
                if (_discardWyrdspawnLocations) {
                    spawner.DiscardAllWyrdSpawns();
                }
            }
        }
    }
}