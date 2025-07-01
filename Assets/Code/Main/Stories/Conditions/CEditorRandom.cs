using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Random")]
    public class CEditorRandom : EditorCondition {
        [Range(0, 100)]
        public int chancePercentage;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CRandom {
                chancePercentage = chancePercentage
            };
        }
    }

    public partial class CRandom : StoryCondition {
        public int chancePercentage;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return (Random.value * 100) <= chancePercentage;
        }
    }
}