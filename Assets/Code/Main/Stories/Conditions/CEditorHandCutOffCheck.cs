using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Hero: Has Hand Cut Off Check")]
    public class CEditorHandCutOffCheck : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHandCutOffCheck();
        }
    }
    
    public partial class CHandCutOffCheck : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return Hero.Current?.HasElement<HeroOffHandCutOff>() ?? false;
        }
    }
}