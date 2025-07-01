using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Items: Give Item Set")]
    public class SEditorGiveItemSet : EditorStep {
        public ItemSet itemSet;
        public bool onlyItems;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SGiveItemSet {
                itemSet = new TemplateReference(itemSet),
                onlyItems = onlyItems
            };
        }
    }

    public partial class SGiveItemSet : StoryStep {
        public TemplateReference itemSet;
        public bool onlyItems;
        
        public override StepResult Execute(Story story) {
            if (onlyItems) {
                itemSet.Get<ItemSet>().ApplyItems(Hero.Current);
            } else {
                itemSet.Get<ItemSet>().ApplyFull();
            }
            return StepResult.Immediate;
        }
    }
}
