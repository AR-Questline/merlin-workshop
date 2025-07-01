using Awaken.TG.Graphics.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class HeroKnockdownMovement: HumanoidMovementBase {
        static readonly int Movement = Animator.StringToHash("Movement");
        public sealed override bool IsNotSaved => true;
        
        // === Fields
        Vector3 _forceDirection;
        float _forceStrength;
        AnimationCurve _knockdownCurve;
        float _knockdownTimer;
        bool _isExiting;
        
        // === Properties
        public override MovementType Type => MovementType.HeroKnockdown;
        public override bool CanCurrentlyBeOverriden => Hero.ShouldDie;
        public override bool RequirementsFulfilled => true;

        protected override void Init() {
            var heroKnockdown = ParentModel.HeroKnockdown;
            if (heroKnockdown == null) {
                ParentModel.ReturnToDefaultMovement();
                return;
            }

            _forceDirection = heroKnockdown.ForceDirection;
            _forceStrength = heroKnockdown.ForceStrength;
            _knockdownCurve = heroKnockdown.KnockdownCurve;
            
            Controller.audioAnimator.SetFloat(Movement, 0);
            Controller.audioAnimator.ResetAllTriggersAndBool();
            Controller.isSwimming = false;
            Controller.isKicking = false;
            Controller.isSlippingFromAI = false;
            IsSprinting = false;
            _isExiting = false;
        }

        public void OnStartExitKnockdown() {
            Controller.Controller.SimpleMove(Vector3.zero);
            Controller.SetVerticalVelocity(DefaultVelocity);
            _isExiting = true;
        }
        
        public override void Update(float deltaTime) {
            Controller.PerformGroundChecks(deltaTime);
            
            Vector3 verticalVel = new Vector3(0, Controller.verticalVelocity, 0) * deltaTime;
            float previousY = Controller.transform.position.y;
            if (_isExiting) {
                Controller.PerformMoveStep(verticalVel);
            } else {
                _knockdownTimer += deltaTime;
                float curveModifier = _knockdownCurve.Evaluate(_knockdownTimer);
                float forceStrength = _forceStrength * curveModifier * deltaTime;
                Controller.PerformMoveStep(_forceDirection * forceStrength + verticalVel);
            }
            FallingSpeed = (Controller.Transform.position.y - previousY) / deltaTime;
            
            Controller.ApplyTransformToTarget();
        }

        protected override void SetupForceExitConditions() { }
        public override void OnControllerColliderHit(ControllerColliderHit hit) { }
    }
}