using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class OverrideSpecialAttack : AttackSpecialAttack {
        public override ushort TypeForSerialization => SavedModels.OverrideSpecialAttack;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= ParentModel.ExitDurationFromAttackAnimations) {
                ParentModel.SetCurrentState(NpcStateType.None, 0.4f);
            }
        }
    }
}