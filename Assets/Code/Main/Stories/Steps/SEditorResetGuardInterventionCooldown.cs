using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Reset Guard Intervention Cooldown"), NodeSupportsOdin]
    public class SEditorResetGuardInterventionCooldown : EditorStep {
        public LocationReference guard;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SResetGuardInterventionCooldown {
                guard = guard,
            };
        }
    }

    public partial class SResetGuardInterventionCooldown : StoryStep {
        public LocationReference guard;
    
        public override StepResult Execute(Story story) {
            var npc = guard.FirstOrDefault(story)?.TryGetElement<NpcElement>();
            if (npc != null) {
                Hero.Current.HeroCombat.ResetGuardInterventionCooldown(npc);
            }
            return StepResult.Immediate;
        }
    }
}