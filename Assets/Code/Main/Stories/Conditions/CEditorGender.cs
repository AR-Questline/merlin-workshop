using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Hero: Gender")]
    public class CEditorGender : EditorCondition {
        public Gender gender;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CGender {
                gender = gender,
            };
        }
    }

    public partial class CGender : StoryCondition {
        public Gender gender;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return Hero.Current.GetGender() == gender;
        }
    }
}