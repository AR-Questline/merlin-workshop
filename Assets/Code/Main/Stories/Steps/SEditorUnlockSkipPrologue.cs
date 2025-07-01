using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.TitleScreen;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Global: Unlock Skip Prologue")]
    public class SEditorUnlockSkipPrologue : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SUnlockSkipPrologue();
        }
    }
    
    public partial class SUnlockSkipPrologue : StoryStep {
        public override StepResult Execute(Story story) {
            PrefMemory.Set(TitleScreenUI.SkipPrologueUnlockKey, true, true);
            return StepResult.Immediate;
        }
    }
}