using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown {
    public partial class KnockdownEnd : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.KnockdownEnd;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(ParentModel is LegsFSM ? HeroStateType.Idle : HeroStateType.None);
                Hero.HeroKnockdown?.KnockdownEnded();
            }
        }

        protected override void OnExit(bool restarted) {
            ResetPositionAfterKnockdownEnd(CurrentState).Forget();
            base.OnExit(restarted);
        }

        async UniTaskVoid ResetPositionAfterKnockdownEnd(AnimancerState stateThatEnded) {
            await UniTask.WaitWhile(() =>
                !HasBeenDiscarded
                && HeroAnimancer.isActiveAndEnabled 
                && stateThatEnded.IsValid 
                && stateThatEnded.Clip != null
                && stateThatEnded.IsPlaying);
            if (HasBeenDiscarded || ParentModel?.HeroAnimancer == null) {
                return;
            }
            ParentModel.HeroAnimancer.transform.localPosition = Vector3.zero;
        }
    }
}