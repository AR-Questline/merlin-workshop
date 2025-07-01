using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if fast start bool is enabled
    /// </summary>
    [Element("Technical: Is Fast Start")]
    public class CEditorIsFastStart : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsFastStart();
        }
    }
    
    public partial class CIsFastStart : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return DebugReferences.FastStart;
        }
    }
}