using Awaken.TG.Graphics;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Weather: Resume rain")]
    public class SEditorResumeRain : EditorStep {
        public bool instant;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SResumeRain {
                instant = instant
            };
        }
    }
    
    public partial class SResumeRain : StoryStep {
        public bool instant;
        
        public override StepResult Execute(Story story) {
            var weatherController = World.Only<WeatherController>();
            weatherController.ResumePrecipitation(instant);
            return StepResult.Immediate;
        }
    }
}
