using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Saving: Manual Auto Save")]
    public class SEditorManualAutoSave : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SManualAutoSave();
        }
    }

    public partial class SManualAutoSave : StoryStep {
        public override StepResult Execute(Story story) {
            World.Services.Get<AutoSaving>().AutoSaveWithRecurringRetry();
            return StepResult.Immediate;
        }
    }
}