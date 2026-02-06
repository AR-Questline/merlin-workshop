using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Hero: In Combat check")]
    public class CEditorHeroInCombat : EditorCondition {
        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHeroInCombat();
        }
    }
    
    public partial class CHeroInCombat : StoryCondition {
        public override bool Fulfilled(Story story, StoryStep step) {
            return Hero.Current.HeroCombat.IsHeroInFight;
        }
    }
}
