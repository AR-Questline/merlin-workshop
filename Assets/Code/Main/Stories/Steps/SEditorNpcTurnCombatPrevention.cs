using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
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
    [Element("NPC/NPC: Change NPC Combat Prevention"), NodeSupportsOdin]
    public class SEditorNpcTurnCombatPrevention : EditorStep {
        public LocationReference locations;
        public bool enablePrevention = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcTurnCombatPrevention {
                locations = locations,
                enablePrevention = enablePrevention
            };
        }
    }

    public partial class SNpcTurnCombatPrevention : StoryStepWithLocationRequirement {
        public LocationReference locations;
        public bool enablePrevention = true;

        protected override LocationReference RequiredLocations => locations;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(enablePrevention);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcTurnCombatPrevention;

            [Saved] bool _enablePrevention;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool enablePrevention) {
                _enablePrevention = enablePrevention;
            }
            
            public override void Execute(Location location) {
                if (!location.TryGetElement<CombatPreventionElement>(out var element)) {
                    if (_enablePrevention) {
                        element = location.AddElement<CombatPreventionElement>();
                    } else {
                        return;
                    }
                }
                element.Toggle(_enablePrevention);
            }
        }
    }
}