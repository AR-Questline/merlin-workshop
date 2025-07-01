using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Time: Set Weather Time")]
    public class SEditorTimeSet : EditorStep {
        public int hour;
        public int minutes;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STimeSet {
                hour = hour,
                minutes = minutes
            };
        }
    }

    public partial class STimeSet : StoryStep {
        public int hour;
        public int minutes;
        
        public override StepResult Execute(Story story) {
            World.Only<GameRealTime>().SetWeatherTime(hour, minutes);
            return StepResult.Immediate;
        }
    }
}