using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcTeleportState : NpcAnimatorState<NpcGeneralFSM> {
        public sealed override bool IsNotSaved => true;
        
        readonly NpcStateType _stateToEnter;
        bool _teleportFinished;

        public override NpcStateType Type => _stateToEnter;
        public override bool CanBeExited => _teleportFinished;

        public new static class Events {
            public static readonly Event<NpcElement, NpcStateType> TeleportAnimationFinished = new(nameof(TeleportAnimationFinished));
        }

        public NpcTeleportState(NpcStateType stateToEnter) {
            _stateToEnter = stateToEnter;
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _teleportFinished = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f && !_teleportFinished) {
                FinishTeleport();
            }
        }

        protected override void OnExit(bool restarted) {
            FinishTeleport();
        }
        
        void FinishTeleport() {
            if (_teleportFinished) {
                return;
            }
            
            _teleportFinished = true;
            Npc.Trigger(Events.TeleportAnimationFinished, _stateToEnter);
        }
        
    }
}