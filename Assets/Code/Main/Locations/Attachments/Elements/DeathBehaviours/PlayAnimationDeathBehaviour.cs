using Awaken.TG.Main.Animations.FSM.Npc.States.General;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class PlayAnimationDeathBehaviour : PostponedRagdollBehaviourBase {
        public override bool UseDeathAnimation => true;
        public override NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
    }
}