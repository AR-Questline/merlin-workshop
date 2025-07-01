using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcEquipWeapon : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcEquipWeapon;

        public override NpcStateType Type => NpcStateType.EquipWeapon;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}