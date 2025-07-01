using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Flag: Check")]
    public class CEditorFlag : EditorCondition, IStoryFlagRef {
        [Tags(TagsCategory.Flag)][HideLabel]
        public string flag = "";
        
        public string FlagRef => flag;
        public string TargetValue => string.Empty;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CFlag {
                flag = flag
            };
        }
    }
    
    public partial class CFlag : StoryCondition {
        public string flag;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return StoryFlags.Get(flag);
        }
    }
}