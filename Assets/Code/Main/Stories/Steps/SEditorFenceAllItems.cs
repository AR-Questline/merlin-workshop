using System.Globalization;
using System.Linq;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Theft: Fence all items")]
    public class SEditorFenceAllItems : EditorStep {
        public float fenceValuePercentageToPay = 0.2f;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SFenceAllItems {
                fenceValuePercentageToPay = fenceValuePercentageToPay
            };
        }
    }

    public partial class SFenceAllItems : StoryStep {
        public float fenceValuePercentageToPay = 0.2f;
        
        public float FenceCost => Hero.Current.Inventory.Items.Where(StolenItemElement.IsStolen).Sum(static item => item.Price * item.Quantity) * fenceValuePercentageToPay;

        public override StepResult Execute(Story story) {
            Hero.Current.Wealth.DecreaseBy(FenceCost);
            foreach (Item item in Hero.Current.Inventory.Items.Where(StolenItemElement.IsStolen)) {
                item.RemoveElementsOfType<StolenItemElement>();
            }
            return StepResult.Immediate;
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            effects.Add($"{CurrencyStatType.Wealth.DisplayName}: {FenceCost.ToString(CultureInfo.InvariantCulture)}");
        }

        public override StepRequirement GetRequirement() {
            return api => Hero.Current.Wealth.ModifiedInt >= FenceCost;
        }
    }
}