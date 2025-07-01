using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Trespassing: Recalculate"), NodeSupportsOdin]
    public class SEditorTrespassingRecalculate : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STrespassingRecalculate { };
        }
    }

    public partial class STrespassingRecalculate : StoryStep {
        public override StepResult Execute(Story story) {
            Hero.Current.Trigger(CrimeUtils.Events.RecalculateTrespassing, true);
            return StepResult.Immediate;
        }
    }
}