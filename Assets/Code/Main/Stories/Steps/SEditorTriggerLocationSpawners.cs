using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Locations.Spawners;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Trigger Location Spawners"), NodeSupportsOdin]
    public class SEditorTriggerLocationSpawners : EditorStep {
        public LocationReference locationReference;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STriggerLocationSpawners {
                locationReference = locationReference
            };
        }
    }

    public partial class STriggerLocationSpawners : StoryStepWithLocationRequirement {
        public LocationReference locationReference;
        
        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution();
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_TriggerLocationSpawners;
            
            public override void Execute(Location location) {
                var spawner = location.TryGetElement<BaseLocationSpawner>();
                if (spawner == null) {
                    Log.Minor?.Error($"No spawner on location: {LogUtils.GetDebugName(location)}");
                    return;
                }
                var manualSpawner = spawner.TryGetElement<ManualSpawner>();
                if (manualSpawner == null) {
                    Log.Minor?.Error($"Spawner on location is not a manual spawner: {LogUtils.GetDebugName(location)}");
                    return;
                }
                manualSpawner.TriggerSpawner();
            }
        }
    }
}