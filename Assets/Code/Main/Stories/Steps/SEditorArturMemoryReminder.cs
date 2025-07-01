using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Arthur memory: Remind")]
    public class SEditorArturMemoryReminder : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SArthturMemoryReminder();
        }
    }
    
    public partial class SArthturMemoryReminder : StoryStep {
        public override StepResult Execute(Story story) {
            story.Hero.Stat(HeroStatType.WyrdWhispers).IncreaseBy(1);
            return StepResult.Immediate;
        }
    }
}