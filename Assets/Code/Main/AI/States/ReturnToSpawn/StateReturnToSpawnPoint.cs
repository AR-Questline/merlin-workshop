using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility.Maths;

namespace Awaken.TG.Main.AI.States.ReturnToSpawn {
    public class StateReturnToSpawnPoint : NpcState<StateReturn> {
        ReturnToSpawnType _returnToSpawnType;
        Wander _wander;
        VelocityScheme _velocityScheme;
        public bool Reached => _returnToSpawnType == ReturnToSpawnType.Returned;
        
        public StateReturnToSpawnPoint(VelocityScheme velocityScheme) {
            _velocityScheme = velocityScheme;
        }
        
        public override void Init() {
            _wander = new Wander(CharacterPlace.Default, _velocityScheme);
            _wander.OnEnd += OnReach;
        }

        protected override void OnEnter() {
            base.OnEnter();
            _returnToSpawnType = ReturnToSpawnType.Returning;
            
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            _wander.UpdateDestination(Npc.LastIdlePosition);
            _wander.UpdateVelocityScheme(_velocityScheme);
            Movement.ChangeMainState(_wander);
        }

        public override void Update(float deltaTime) {
            AI.AlertStack.TopDecreaseRate = 2;
        }

        protected override void OnExit() {
            base.OnExit();
            Movement.ResetMainState(_wander);
            _returnToSpawnType = ReturnToSpawnType.None;
            AI.AlertStack.Reset();
        }
        
        void OnReach() {
            if (Npc.Coords.EqualsApproximately(_wander.Destination.Position, _wander.Destination.Radius * 2f)) {
                _returnToSpawnType = ReturnToSpawnType.Returned;
            } else {
                Movement.ChangeMainState(_wander);
            }
        }

        enum ReturnToSpawnType : byte {
            None = 0,
            Returning = 1,
            Returned = 2,
        }
    }
}