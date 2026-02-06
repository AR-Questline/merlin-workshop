using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.TitleScreen.Loading;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Technical: Is Loading Game (not changing scene)")]
    public class CEditorIsLoadingGame : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsLoadingGame();
        }
    }

    public partial class CIsLoadingGame : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return LoadingScreenUI.IsFullyLoading;
        }
    }
}
