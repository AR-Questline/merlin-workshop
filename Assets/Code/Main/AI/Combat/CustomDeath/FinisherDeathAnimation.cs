using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public class FinisherDeathAnimations : PostponedRagdollBehaviourBase {
        RagdollEnableData _data;
        
        public override bool UseDeathAnimation => true;
        public override NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Finisher;
        
        protected override RagdollEnableData RagdollData => _data;
        
        public void Setup(RagdollEnableData data) {
            _data = data;
        }
    }
}
