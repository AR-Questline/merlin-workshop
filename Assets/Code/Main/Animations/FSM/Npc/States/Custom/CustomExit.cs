using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public partial class CustomExit : NpcAnimatorState<NpcCustomActionsFSM>, IAnimatorBridgeStateProvider {
        public override ushort TypeForSerialization => SavedModels.CustomExit;

        public override NpcStateType Type => NpcStateType.CustomExit;
        public override bool CanUseMovement => false;
        public bool AlwaysAnimate => true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            if (!NpcAnimancer.isActiveAndEnabled) {
                OnAnimatorDisabled();
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.None);
            }
        }

        protected override void OnExit(bool restarted) {
            Npc.Trigger(NpcCustomActionsFSM.Events.CustomStateExited, true);
            base.OnExit(restarted);
        }

        public void OnAnimatorDisabled() {
            ParentModel.SetCurrentState(NpcStateType.None, 0);
        }
    }
}