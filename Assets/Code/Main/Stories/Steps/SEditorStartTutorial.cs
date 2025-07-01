using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Tutorial: Spawn")]
    public class SEditorStartTutorial : EditorStep {
        [NodeEnum]
        public TutKeys tutorial;
        public bool afterStoryEnd = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStartTutorial {
                tutorial = tutorial,
                afterStoryEnd = afterStoryEnd
            };
        }
    }

    public partial class SStartTutorial : StoryStep {
        public TutKeys tutorial;
        public bool afterStoryEnd = true;
        
        public override StepResult Execute(Story story) {
            TutorialMaster master = World.Any<TutorialMaster>();
            if (master != null) {
                if (afterStoryEnd) {
                    story.ListenTo(Model.Events.AfterDiscarded, () => RunStory(master), master);
                } else {
                    RunStory(master);
                }
            }

            return StepResult.Immediate;
        }

        void RunStory(TutorialMaster master) {
            using var tutorialCreationScope = TutorialSequence.Creation;
            TutorialSequence.Create().Append(tutorial, master.RunStep(out _));
        }
    }
}