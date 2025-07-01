using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Hero: In wyrdness")]
    public class CEditorHeroInWyrdness : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHeroInWyrdness();
        }
    }

    public partial class CHeroInWyrdness : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return Hero.Current != null && !Hero.Current.IsSafeFromWyrdness;
        }
    }
}