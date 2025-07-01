using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Conditions.GameModes {
    [Element("GameMode: Demo")]
    public class CEditorIsDemo : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsDemo();
        }
    }
    
    public partial class CIsDemo : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return GameMode.IsDemo;
        }
    }
    
}