using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Turn From Ghost"), NodeSupportsOdin]
    public class SEditorNpcTurnFromGhost : EditorStep {
        public LocationReference locations;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcTurnFromGhost {
                locations = locations
            };
        }
    }

    public partial class SNpcTurnFromGhost : StoryStepWithLocationRequirement {
        public LocationReference locations;
        
        protected override LocationReference RequiredLocations => locations;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution();
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcTurnFromGhost;
            
            public override void Execute(Location location) {
                NpcElement npcElement = location.TryGetElement<NpcElement>();
                NpcGhostElement npcGhostElement = npcElement?.TryGetElement<NpcGhostElement>();
                if (npcGhostElement != null) {
                    npcGhostElement.RevertChangesAndDiscard().Forget();
                }
            }
        }
    }
}