using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Turn Into Ghost"), NodeSupportsOdin]
    public class SEditorNpcTurnIntoGhost : EditorStep {
        public LocationReference locations;
        public bool revertable;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcTurnIntoGhost {
                locations = locations,
                revertable = revertable
            };
        }
    }

    public partial class SNpcTurnIntoGhost : StoryStepWithLocationRequirement {
        public LocationReference locations;
        public bool revertable;
        
        protected override LocationReference RequiredLocations => locations;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(revertable);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcTurnIntoGhost;

            [Saved] bool _revertable;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool revertable) {
                _revertable = revertable;
            }
            
            public override void Execute(Location location) {
                NpcElement npcElement = location.TryGetElement<NpcElement>();
                if (npcElement != null && !npcElement.HasElement<NpcGhostElement>()) {
                    npcElement.AddElement(new NpcGhostElement(_revertable));
                }
            }
        }
    }
}