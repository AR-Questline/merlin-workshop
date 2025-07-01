using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcStaggerExit : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcStaggerExit;

        public override NpcStateType Type => NpcStateType.StaggerExit;
        public override bool ResetMovementSpeed => true;
        
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
                Npc.Trigger(EnemyBaseClass.Events.StaggerAnimExitEnded, true);
            }
        }
    }
}