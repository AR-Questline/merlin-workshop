using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Sharpening: Open")]
    public class SEditorOpenSharpeningUI : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenSharpeningUI();
        }
    }

    public partial class SOpenSharpeningUI : StoryStep {
        public override StepResult Execute(Story story) {
            if (story.Hero != null) {
                var gearUpgradeUI = GemsUI.OpenSharpeningUI();
                var result = new StepResult();
                gearUpgradeUI.ListenTo(Model.Events.AfterDiscarded, _ => OnGearUpgradesUIClose(result), story);
                return result;
            }
            
            Log.Important?.Error($"Hero {story.Hero} is invalid");
            return StepResult.Immediate;
        }

        void OnGearUpgradesUIClose(StepResult result) {
            result.Complete();
        }
        
        public override string GetKind(Story story) {
            return "GearUpgradesUI";
        }
    }
}