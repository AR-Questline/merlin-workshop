using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class PhaseTransition : NpcAnimatorState<NpcGeneralFSM> {
        public sealed override bool IsNotSaved => true;

        readonly NpcStateType _stateToEnter;
        bool _transitionFinished;

        public override NpcStateType Type => _stateToEnter;
        public override bool CanBeExited => _transitionFinished;
        public override bool CanUseMovement => false;

        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<NpcElement, bool> TransitionFinished = new(nameof(TransitionFinished));
        }

        public PhaseTransition(NpcStateType stateToEnter) {
            _stateToEnter = stateToEnter;
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _transitionFinished = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                FinishTransition();
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }

        protected override void OnExit(bool restarted) {
            FinishTransition();
        }
        
        void FinishTransition() {
            if (_transitionFinished) {
                return;
            }
            
            _transitionFinished = true;
            Npc.Trigger(Events.TransitionFinished, true);
        }
    }
}