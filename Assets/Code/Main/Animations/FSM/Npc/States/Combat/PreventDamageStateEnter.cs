using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class PreventDamageStateEnter : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.PreventDamageStateEnter;

        public override NpcStateType Type => NpcStateType.PreventDamageEnter;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.5f) {
                ParentModel.SetCurrentState(NpcStateType.PreventDamageLoop);
            }
        }
    }
}
