using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.VisualGraphUtils;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Sarras Sickle: Increase Charge")]
    public class SEditorIncreaseSarrasSickleCharge : EditorStep {
        public float chargeAmount = 0.2f;
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SIncreaseSarrasSickleCharge {
                chargeAmount = chargeAmount
            };
        }
    }
    
    public partial class SIncreaseSarrasSickleCharge : StoryStep {
        public float chargeAmount;
        
        public override StepResult Execute(Story story) {
            VGUtils.IncreaseSarrasSickleCharge(chargeAmount);
            return StepResult.Immediate;
        }
    }
}