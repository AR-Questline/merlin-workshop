using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Saving: Manual Auto Save")]
    public class SEditorManualAutoSave : EditorStep {
        public bool forceSaveRightNow = false;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SManualAutoSave() {
                forceSaveRightNow = forceSaveRightNow
            };
        }
    }

    public partial class SManualAutoSave : StoryStep {
        public bool forceSaveRightNow;

        public override StepResult Execute(Story story) {
            if (forceSaveRightNow) {
                var stepResult = new StepResult();
                World.Services.Get<AutoSaving>().ForceAutoSaveWithRecurringRetry(() => stepResult.Complete());
                return stepResult;
            } 
            
            World.Services.Get<AutoSaving>().AutoSaveWithRecurringRetry();
            return StepResult.Immediate;
        }
    }
}