using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcParried : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcParried;

        public override NpcStateType Type => NpcStateType.Parried;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.2f) {
                ParentModel.SetCurrentState(NpcStateType.Idle, 0.15f);
                Npc.Trigger(EnemyBaseClass.Events.ParriedAnimEnded, true);
            }
        }
    }
}