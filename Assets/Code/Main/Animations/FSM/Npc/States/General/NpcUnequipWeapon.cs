using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcUnequipWeapon : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcUnequipWeapon;

        public override NpcStateType Type => NpcStateType.UnequipWeapon;
        public override bool CanBeExited => RemainingDuration <= 0.3f;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}