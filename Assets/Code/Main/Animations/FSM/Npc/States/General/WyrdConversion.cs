using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Saving;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public sealed partial class WyrdConversion : NpcAnimatorState<NpcGeneralFSM> {
        public override bool IsNotSaved => true;

        readonly bool _into;
        bool _canBeExited;

        public override NpcStateType Type => _into ? NpcStateType.WyrdConversionIn : NpcStateType.WyrdConversionOut;
        public override bool CanBeExited => _canBeExited;
        public override bool CanUseMovement => false;

        public WyrdConversion(bool into) {
            _into = into;
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canBeExited = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                _canBeExited = true;
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}