using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public partial class BlockLoop : BlockStateBase {
        MixerState<Vector2> _mixerState;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Block;
        public override HeroStateType Type => HeroStateType.BlockLoop;
        public override HeroStateType StateToEnter => UseBlockWithoutShield ? HeroStateType.BlockLoopWithoutShield : HeroStateType.BlockLoop;

        public override float EntryTransitionDuration => 0.15f;
        protected override bool HeadBobbingDependent => true;
        protected float BlendSpeed => AnimancerUtils.BlendTreeBlendSpeed();
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _mixerState = (MixerState<Vector2>)CurrentState;
            
            if (!Hero.HasElement<HeroBlock>()) {
                Hero.AddElement(new HeroBlock());
            }
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (_mixerState != null) {
                var mixerParam = new Vector2(Hero.RelativeVelocity.y, Hero.RelativeVelocity.x);
                _mixerState.Parameter = Vector2.MoveTowards(_mixerState.Parameter, mixerParam, BlendSpeed * deltaTime);
            }
            
            if (!ParentModel.BlockHeld) {
                ParentModel.SetCurrentState(HeroStateType.BlockExit, 0.1f);
            }
        }
    }
}