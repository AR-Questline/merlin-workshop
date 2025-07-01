using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/Presence: Temporary Availability (Deactivate)")]
    public class SEditorDeactivateTemporarilyActivatedNpcPresence : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDeactivateTemporarilyActivatedNpcPresence();
        }
    }

    public partial class SDeactivateTemporarilyActivatedNpcPresence : StoryStep {
        public override StepResult Execute(Story story) {
            story.TryGetElement<StoryBasedNpcPresences>()?.Discard();
            return StepResult.Immediate;
        }
    }
}