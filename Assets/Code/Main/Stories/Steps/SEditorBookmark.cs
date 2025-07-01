using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Branch/Story: Bookmark")]
    public class SEditorBookmark : EditorStep, IStorySettings {
        
        public string flag;

        public bool storySettings;
        [ShowIf(nameof(storySettings))] public bool involveHero = true;
        [ShowIf(nameof(storySettings))] public bool involveAI = true;
     
        public bool InvolveHero => involveHero;
        public bool InvolveAI => involveAI;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBookmark {
                flag = flag,
                storySettings = storySettings,
                involveHero = involveHero,
                involveAI = involveAI
            };
        }
    }

    public partial class SBookmark : StoryStep {
        public string flag;

        public bool storySettings;
        public bool involveHero;
        public bool involveAI;
        
        public override StepResult Execute(Story story) {
            return StepResult.Immediate;
        }
    }
}
