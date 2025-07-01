using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Trespassing: Ignore Time To Crime"), NodeSupportsOdin]
    public class SEditorTrespassingIgnoreTimeToCrime : EditorStep {
        public LocationReference guard;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STrespassingIgnoreTimeToCrime();
        }
    }

    public partial class STrespassingIgnoreTimeToCrime : StoryStep {
        public override StepResult Execute(Story story) {
            Hero.Current.TryGetElement<TrespassingTracker>()?.IgnoreTimeToCrime();
            return StepResult.Immediate;
        }
    }
}