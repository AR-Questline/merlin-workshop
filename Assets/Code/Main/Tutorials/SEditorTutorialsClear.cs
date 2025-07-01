using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;

namespace Awaken.TG.Main.Tutorials {
    [Element("Technical/Tutorial: Clear Tutorials")]
    public class SEditorTutorialsClear : EditorStep {
        public SequenceKey[] sequencesToClear = TutorialKeys.AllSequenceKeys;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STutorialsClear {
                sequencesToClear = sequencesToClear
            };
        }
    }

    public partial class STutorialsClear : StoryStep {
        public SequenceKey[] sequencesToClear;
        
        public override StepResult Execute(Story story) {
            TutorialSequence.Kill(true, sequencesToClear);
            return StepResult.Immediate;
        }
    }
}