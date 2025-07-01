using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Dialogue/Hero Camera: Change focus"), NodeSupportsOdin]
    public class SEditorLocationChangeFocus : EditorStep {
        public LocationReference focusTarget;
        public SLocationChangeFocus.FocusOverrideType focusType = SLocationChangeFocus.FocusOverrideType.Normal;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationChangeFocus {
                focusTarget = focusTarget,
                focusType = focusType
            };
        }
    }

    public partial class SLocationChangeFocus : StoryStep {
        public LocationReference focusTarget;
        public FocusOverrideType focusType = FocusOverrideType.Normal;
        
        public override StepResult Execute(Story story) {
            if (focusType == FocusOverrideType.Reset) {
                story.TryGetElement<StoryInteractionFocusOverride>()?.Discard();
                return StepResult.Immediate;
            }

            var location = focusTarget.FirstOrDefault(story);
            if (location) {
                if (focusType == FocusOverrideType.Normal) {
                    story.ChangeFocusedLocation(location);
                    return StepResult.Immediate;
                }

                if (focusType == FocusOverrideType.Force) {
                    var focusOverride = story.TryGetElement<StoryInteractionFocusOverride>();
                    var focusTransform = location.TryGetElement<IWithLookAt>()?.LookAtTarget ?? location.ViewParent; 
                    if (focusOverride != null) {
                        focusOverride.SetFocus(focusTransform);
                    } else {
                        story.AddElement(new StoryInteractionFocusOverride(focusTransform));
                    }
                    return StepResult.Immediate;
                }
            }
            return StepResult.Immediate;
        }

        public enum FocusOverrideType : byte {
            Normal,
            Force,
            Reset
        }
    }
}
