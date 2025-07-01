using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcEquipRangedWeapon : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcEquipRangedWeapon;

        public override NpcStateType Type => NpcStateType.EquipRangedWeapon;
        
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}