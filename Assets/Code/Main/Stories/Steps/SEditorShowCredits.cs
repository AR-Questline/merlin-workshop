using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Credits: Show")]
    public class SEditorShowCredits : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SShowCredits();
        }
    }
    
    public partial class SShowCredits : StoryStep {
        public override StepResult Execute(Story story) {
            StepResult result = new();
            Credits credits = World.Add(new Credits());
            credits.ListenTo(Model.Events.AfterDiscarded, () => result.Complete(), story);
            return result;
        }
    }
}
