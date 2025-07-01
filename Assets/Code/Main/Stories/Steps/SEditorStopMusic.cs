using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Audio/Audio: Stop Story Music"), NodeSupportsOdin]
    public class SEditorStopMusic : EditorStep {
        public AudioType managerType;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStopMusic {
                managerType = managerType
            };
        }
    }

    public partial class SStopMusic : StoryStep {
        public AudioType managerType;
        
        public override StepResult Execute(Story story) {
            World.All<StoryMusic>().ToArraySlow().ForEach(s => {
                if (s.managerType == managerType) {
                    s.Discard();
                }
            });
            return StepResult.Immediate;
        }
    }
}