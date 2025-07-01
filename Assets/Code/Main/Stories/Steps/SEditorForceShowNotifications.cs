using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Notifications: Force Show")]
    public class SEditorForceShowNotifications : EditorStep {
        public bool enable = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SForceShowNotifications {
                enable = enable
            };
        }
    }

    public partial class SForceShowNotifications : StoryStep {
        public bool enable = true;
        
        public override StepResult Execute(Story story) {
            var element = story.TryGetElement<ForceShowNotificationsStoryElement>();
            switch (enable) {
                case true when element == null:
                    story.AddElement(new ForceShowNotificationsStoryElement());
                    break;
                case false when element != null:
                    element.Discard();
                    break;
            }
            return StepResult.Immediate;
        }

        public partial class ForceShowNotificationsStoryElement : Element<Story> {
            protected override void OnInitialize() {
                foreach (var buffer in World.All<AdvancedNotificationBuffer>()) {
                    buffer.ChangeForceVisible(true);
                }
            }

            protected override void OnDiscard(bool fromDomainDrop) {
                if (fromDomainDrop) {
                    return;
                }
                foreach (var buffer in World.All<AdvancedNotificationBuffer>()) {
                    buffer.ChangeForceVisible(false);
                }
            }
        }
    }
}