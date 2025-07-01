using Awaken.TG.Graphics;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Weather: Set Preset")]
    public class SEditorWeatherSet : EditorStep {
        [Tooltip("Presets are set in Game Constants")]
        public int preset;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SWeatherSet {
                preset = preset
            };
        }
    }

    public partial class SWeatherSet : StoryStep {
        public int preset;
        
        public override StepResult Execute(Story story) {
            World.Only<WeatherController>().SetPreset(preset);
            return StepResult.Immediate;
        }
    }
}