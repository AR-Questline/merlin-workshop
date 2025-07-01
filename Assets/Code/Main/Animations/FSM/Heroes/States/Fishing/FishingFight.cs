using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingFight : HeroAnimatorState<FishingFSM> {
        MixerState<Vector2> _mixerState;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingFight;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _mixerState = (MixerState<Vector2>)CurrentState;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (_mixerState != null) {
                _mixerState.Parameter = new Vector2(0, ParentModel.fishingFightWeight);
            }
        }
    }
}