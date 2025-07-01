using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Hero: Lose All Wyrd Souls")]
    public class SEditorLoseAllWyrdSoulFragments : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLoseAllWyrdSoulFragments();
        }
    }

    public partial class SLoseAllWyrdSoulFragments : StoryStep {
        public override StepResult Execute(Story story) {
            Hero.Current.Development.WyrdSoulFragments.LockAll();
            return StepResult.Immediate;
        }
    }
}