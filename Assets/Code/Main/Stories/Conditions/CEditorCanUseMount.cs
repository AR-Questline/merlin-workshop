using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero can use his mount.
    /// </summary>
    [Element("Technical: Can Use Mount")]
    public class CEditorCanUseMount : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CCanUseMount();
        }
    }
    
    public partial class CCanUseMount : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return Hero.Current?.CanUseMount() ?? false;
        }
    }
}