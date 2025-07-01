using Awaken.TG.Graphics.Animations;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Bow {
    public partial class BowCancelDraw : HeroAnimatorState<BowFSM>, IStateWithModifierAttackSpeed {
        static readonly int FireStrength = Animator.StringToHash("FireStrength");
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.BowCancelDraw;
        public override bool CanPerformNewAction => TimeElapsedNormalized <= 0.01f;
        public override float EntryTransitionDuration => ParentModel.WasCanceledWhenInBowHold ? 0.25f : 0f;
        public float AttackSpeed => ParentModel.GetAttackSpeed(false) * -1;
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.WasCanceledWhenInBowHold ? 1 : previousNormalizedTime;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.EndSlowModifier();
            Hero.TryGetElement<ForcedInputFromCode>()?.Discard();
            
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnBowDrawCancel(1 - OffsetNormalizedTime(previousStateNormalizedTime));
            }
            
            AnimatorUtils.StartProcessingAnimationSpeed(ParentModel.HeroAnimancer, ParentModel.AnimancerLayer, ParentModel.LayerType, StateToEnter, false, WeaponRestriction.None);
        }

        protected override void OnUpdate(float deltaTime) {
            if (CurrentState != null) {
                CurrentState.Speed = AttackSpeed;
            }
            
            if (TimeElapsedNormalized <= 0f) {
                ParentModel.SetCurrentState(HeroStateType.Idle, 0f);
            }
        }

        protected override void OnExit(bool restarted) {
            Animator.SetFloat(FireStrength, 0);
            Hero.Trigger(Awaken.TG.Main.Heroes.Hero.Events.StopProcessingAnimationSpeed, true);
        }
    }
}