using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.VisualGraphUtils;
using System.Linq;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Abstraction to change locations in world
    /// </summary>
    [Element("Location/Location: Trigger Visual Scripting"), NodeSupportsOdin]
    public class SEditorTriggerLocationVS : EditorStep {
        public VSCustomEvent actionType = VSCustomEvent.Interact;
        public LocationReference locationReference;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STriggerLocationVS {
                actionType = actionType,
                locationReference = locationReference
            };
        }
    }

    public partial class STriggerLocationVS : StoryStep {
        public VSCustomEvent actionType = VSCustomEvent.Interact;
        public LocationReference locationReference;
        
        public override StepResult Execute(Story story) {
            locationReference.MatchingLocations(story).ToList().ForEach(l => l?.TriggerVisualScriptingEvent(actionType));
            return StepResult.Immediate;
        }
    }
}