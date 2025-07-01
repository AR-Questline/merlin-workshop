using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class PreventDamageStateLoop : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.PreventDamageStateLoop;

        public override NpcStateType Type => NpcStateType.PreventDamageLoop;
        public override bool CanUseMovement => false;
        public override bool CanBeExited => _isPreventingFinished;
        
        bool _isPreventingFinished;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _isPreventingFinished = false;
        }
        
        public void Leave(NpcStateType exitState) {
            _isPreventingFinished = true;
            ParentModel.SetCurrentState(exitState);
        }
    }
}
