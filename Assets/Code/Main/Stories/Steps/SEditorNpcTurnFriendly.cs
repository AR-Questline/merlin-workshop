using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Turn Friendly"), NodeSupportsOdin]
    public class SEditorNpcTurnFriendly : EditorStep {
        public LocationReference locations;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcTurnFriendly {
                locations = locations
            };
        }
    }

    public partial class SNpcTurnFriendly : StoryStepWithLocationRequirement {
        public LocationReference locations;

        protected override LocationReference RequiredLocations => locations;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution();
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcTurnFriendly;
            
            public override void Execute(Location location) {
                location.TryGetElement<NpcElement>()?.TurnFriendlyTo(AntagonismLayer.Story, Hero.Current);
            }
        }
    }
}