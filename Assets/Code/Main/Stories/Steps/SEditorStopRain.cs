using Awaken.TG.Graphics;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Weather: Stop rain")]
    public class SEditorStopRain : EditorStep {
        public bool instant;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStopRain {
                instant = instant
            };
        }
    }
    
    public partial class SStopRain : StoryStep {
        public bool instant;
        
        public override StepResult Execute(Story story) {
            var weatherController = World.Only<WeatherController>();
            weatherController.StopPrecipitation(instant);
            return StepResult.Immediate;
        }
    }
}
