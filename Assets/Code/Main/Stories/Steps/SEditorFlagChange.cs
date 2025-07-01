using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    /// <summary>
    /// Sets a global flag than can be checked elsewhere.
    /// </summary>
    [Element("Flag: Change"), NodeSupportsOdin]
    public class SEditorFlagChange : EditorStep, IStoryFlagRef {
        [HideLabel][Tags(TagsCategory.Flag)]
        public string flag = "";
        public bool newState = true;
        
        public string FlagRef => flag;
        public string TargetValue => newState.ToString();

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SFlagChange {
                flag = flag,
                newState = newState
            };
        }
    }

    public partial class SFlagChange : StoryStep {
        public string flag = "";
        public bool newState = true;
        
        public override StepResult Execute(Story story) {
            StoryFlags.Set(flag, newState);
            return StepResult.Immediate;
        }
    }
}