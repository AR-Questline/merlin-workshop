using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.TPP {
    public partial class TppMovementState : MovementState<LegsFSM> {
        const float InAttackMovementExitBlendDuration = 0.5f;
        
        // === Fields
        bool _isInAttack;
        
        // === Properties
        public override HeroStateType StateToEnter => Hero.Mounted
            ? HeroStateType.HorseRidingMovement
            : Hero.IsSwimming
                ? HeroStateType.LegsSwimmingMovement
                : ParentModel.ShouldCrouch
                    ? HeroStateType.CrouchedMovement
                    : _isInAttack
                        ? HeroStateType.InAttackMovement
                        : base.StateToEnter;
        protected override bool ExitToIdleCondition => !Hero.IsSwimming && !Hero.Mounted && base.ExitToIdleCondition;
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();
        protected override float? ReEnterBlendDuration => _lastMovementState == HeroStateType.InAttackMovement
            ? InAttackMovementExitBlendDuration
            : null;

        protected override void OnInitialize() {
            Hero.ListenTo(Hero.Events.HeroAttacked, OnAttackStarted, this);
            Hero.ListenTo(Hero.Events.StopProcessingAnimationSpeed, OnAttackEnded, this);
            base.OnInitialize();
        }
        
        protected override void UpdateMixerParameter(float deltaTime) {
            if (_mixerState != null) {
                Vector2 mixerParam;
                if (Hero.Mounted) {
                    var velocity = ((MountedMovement)Hero.MovementSystem).MountView.AnimatorParams;
                    mixerParam = new Vector2(velocity.y, velocity.x);
                } else {
                    mixerParam = new Vector2(Hero.RelativeVelocity.y, Hero.RelativeVelocity.x);
                }

                _mixerState.Parameter = Vector2.MoveTowards(_mixerState.Parameter, mixerParam, BlendSpeed * deltaTime);
            }
        }

        void OnAttackStarted(bool _) => _isInAttack = true;
        void OnAttackEnded(bool _) => _isInAttack = false;
    }
}