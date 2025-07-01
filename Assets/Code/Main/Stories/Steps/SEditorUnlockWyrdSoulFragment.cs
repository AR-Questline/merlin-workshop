using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Unlock Wyrd Soul")]
    public class SEditorUnlockWyrdSoulFragment : EditorStep {
        public WyrdSoulFragmentType fragmentType;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SUnlockWyrdSoulFragment {
                fragmentType = fragmentType
            };
        }
    }

    public partial class SUnlockWyrdSoulFragment : StoryStep {
        public WyrdSoulFragmentType fragmentType;
        
        public override StepResult Execute(Story story) {
            Hero.Current.Development.WyrdSoulFragments.Unlock(fragmentType);
            return StepResult.Immediate;
        }
    }
}