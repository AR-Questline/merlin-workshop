using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public partial class BlockStart : BlockStateBase {
        const float ExitBlockStartAfter = 0.5f;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Block;
        public override HeroStateType Type => HeroStateType.BlockStart;
        public override HeroStateType StateToEnter => UseBlockWithoutShield ? HeroStateType.BlockStartWithoutShield : HeroStateType.BlockStart;
        public override bool CanPerformNewAction => TimeElapsedNormalized > 0.5f;
        public override float EntryTransitionDuration => 0.1f;
        bool _canParry = true;
        bool _exitsToParry;
        bool _postponeEventFired;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.ResetBothProlongs();
            _canParry = true;
            _exitsToParry = false;
            _postponeEventFired = false;
            CurrentState.Speed = Hero.CharacterStats.BlockPrepareSpeed;
            
            Hero.Trigger(HeroHealthElement.Events.HeroParryPostponeWindowStarted, true);
        }

        protected override void OnUpdate(float deltaTime) {
            if (!ParentModel.BlockDown && !ParentModel.BlockHeld) {
                _exitsToParry = _canParry;
                ParentModel.SetCurrentState(_canParry ? HeroStateType.BlockParry : HeroStateType.BlockExit, _canParry ? 0.1f : null);
                return;
            }
            
            if (TimeElapsedNormalized >= ExitBlockStartAfter) {
                ParentModel.SetCurrentState(HeroStateType.BlockLoop);
            }
            
            _canParry = !ParentModel.BlockLongHeld;
            if (!_canParry) {
                TriggerPostponeWindowEnd();
            }
        }

        protected override void OnExit(bool restarted) {
            if (!_exitsToParry) {
                TriggerPostponeWindowEnd();
            }
        }

        void TriggerPostponeWindowEnd() {
            if (_postponeEventFired) {
                return;
            }
            _postponeEventFired = true;
            
            if (!Hero.HasElement<HeroBlock>()) {
                Hero.AddElement(new HeroBlock());
            }
            
            Hero.Trigger(HeroHealthElement.Events.HeroParryPostponeWindowEnded, false);
        }
    }
}