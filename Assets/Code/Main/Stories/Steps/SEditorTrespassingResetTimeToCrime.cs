using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Trespassing: Reset Time To Crime"), NodeSupportsOdin]
    public class SEditorTrespassingResetTimeToCrime : EditorStep {
        public LocationReference guard;
        public bool overrideTimeToCrime;
        [ShowIf(nameof(overrideTimeToCrime))] public float timeToCrime;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STrespassingResetTimeToCrime {
                guard = guard,
                overrideTimeToCrime = overrideTimeToCrime,
                timeToCrime = timeToCrime
            };
        }
    }
    
    public partial class STrespassingResetTimeToCrime : StoryStep {
        public LocationReference guard;
        public bool overrideTimeToCrime;
        public float timeToCrime;
        
        public override StepResult Execute(Story story) {
            Hero.Current.TryGetElement<TrespassingTracker>()?.ResetTimeToCrime(overrideTimeToCrime ? timeToCrime : null);
            return StepResult.Immediate;
        }
    }
}