using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Shared {
    public partial class EmptyState : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.Empty;
        
        public override void Enter(float previousStateNormalizedTime, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            Entered = true;

            Animator.GetComponentsInChildren<CharacterHandBase>().ForEach(h => h.OnUnEquippingEnded());
            ParentModel.ResetInput();
            onNodeLoaded?.Invoke(null);
        }
    }
}