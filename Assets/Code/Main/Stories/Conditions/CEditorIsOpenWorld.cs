using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if we're currently in an open world scene.
    /// </summary>
    [Element("Technical: Is Open World")]
    public class CEditorIsOpenWorld : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsOpenWorld();
        }
    }
    
    public partial class CIsOpenWorld : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return World.Services.Get<SceneService>().IsOpenWorld;
        }
    }
}