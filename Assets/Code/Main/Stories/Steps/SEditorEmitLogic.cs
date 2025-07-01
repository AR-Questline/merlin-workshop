using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Emit logic"), NodeSupportsOdin]
    public class SEditorEmitLogic : EditorStep {
        public LocationReference target;
        [Space]
        public SEmitLogic.TargetState targetState;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SEmitLogic {
                target = target,
                targetState = targetState
            };
        }
    }

    public partial class SEmitLogic : StoryStep {
        public LocationReference target;
        public TargetState targetState;

        public override StepResult Execute(Story story) {
            foreach (Location matchingLocation in target.MatchingLocations(story)) {
                if (matchingLocation.TryGetElement<LogicReceiverAction>() is not { } receiver) {
                    receiver = matchingLocation.AddElement<LogicReceiverAction>();
                }
                if (targetState == TargetState.Enabled)
                    receiver.OnActivation(true);
                else if (targetState == TargetState.Disabled)
                    receiver.OnActivation(false);
                else if (targetState == TargetState.Toggle) 
                    receiver.OnActivation(!receiver.IsActive);
            }
            return StepResult.Immediate;
        }
        
        public enum TargetState : byte {
            Toggle = 0,
            Enabled = 1,
            Disabled = 2
        }
    }
}