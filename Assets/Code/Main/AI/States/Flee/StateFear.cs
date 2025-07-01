using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Grounds;

namespace Awaken.TG.Main.AI.States.Flee {
    public class StateFear : NpcState<StateFlee> {
        const float MaxDelay = 0.7f;

        SnapToPositionAndRotate _movement;
        bool _active;
        float _delay;

        protected override void OnEnter() {
            base.OnEnter();
            _movement = new SnapToPositionAndRotate(Npc.Coords, Npc.Forward(), null);
            _delay = RandomUtil.UniformFloat(0, MaxDelay);
            _active = false;
            
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            Movement.ChangeMainState(_movement);
        }

        protected override void OnExit() {
            base.OnExit();
            Movement.ResetMainState(_movement);
            _movement = null;
        }

        public override void Update(float deltaTime) {
            if (_active) {
                return;
            }
            
            _delay -= deltaTime;
            if (_delay <= 0) {
                _active = true;
                Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Fear);
            }
        }
    }
}