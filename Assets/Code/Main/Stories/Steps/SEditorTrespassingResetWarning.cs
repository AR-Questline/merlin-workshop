using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Trespassing: Reset Warning"), NodeSupportsOdin]
    public class SEditorTrespassingResetWarning : EditorStep {
        public LocationReference guard;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STrespassingResetWarning {
                guard = guard,
            };
        }
    }
    
    public partial class STrespassingResetWarning : StoryStep {
        public LocationReference guard;

        public override StepResult Execute(Story story) {
            Hero.Current.TryGetElement<TrespassingTracker>()?.ResetWarning();
            return StepResult.Immediate;
        }
    }
}