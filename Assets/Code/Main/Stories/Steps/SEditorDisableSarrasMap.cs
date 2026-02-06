using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Map: Disable Sarras")]
    public class SEditorDisableSarrasMap : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDisableSarrasMap();
        }
    }
    
    public partial class SDisableSarrasMap : StoryStep {
        public override StepResult Execute(Story story) {
            World.Services.Get<MapService>().LockSarrasMap();
            return StepResult.Immediate;
        }
    }
}