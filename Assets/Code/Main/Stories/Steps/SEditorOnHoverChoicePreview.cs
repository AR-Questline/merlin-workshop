using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Utils;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hover: Show Choice Preview")]
    public class SEditorOnHoverChoicePreview : EditorStep {
        [LocStringCategory(Category.UI)]
        public LocString proficienciesSetName;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOnHoverChoicePreview {
                proficienciesSetName = proficienciesSetName
            };
        }
    }

    public partial class SOnHoverChoicePreview : StoryStep {
        public LocString proficienciesSetName;
        
        public override StepResult Execute(Story story) {
            return StepResult.Immediate;
        }

        public override void AppendHoverInfo(Story story, ref StructList<IHoverInfo> hoverInfo) {
            foreach (var step in new StoryBranchIterator(parentChapter)) {
                if (step is SStatChange statChange) {
                    if (statChange.AffectedStat is ProfStatType profStatType) {
                        hoverInfo.Add(new ProficiencyHoverInfo(proficienciesSetName, profStatType, statChange.statValue));
                    }
                }
                if (step is SChangeItemsQuantity changeStep) {
                    foreach (var itemSpawningData in changeStep.itemTemplateReferenceQuantityPairs) {
                        hoverInfo.Add(new ItemHoverInfo(proficienciesSetName, itemSpawningData.ItemTemplate(null)));
                    }
                }
            }
        }
    }
}