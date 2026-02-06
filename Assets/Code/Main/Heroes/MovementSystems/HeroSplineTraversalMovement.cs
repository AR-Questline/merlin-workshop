using Awaken.TG.Graphics.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class HeroSplineTraversalMovement : HeroMovementSystem {
        const float PositionUpdateInterval = 0.5f;
        static readonly int Movement = Animator.StringToHash("Movement");
        public sealed override bool IsNotSaved => true;
        
        public override MovementType Type => MovementType.FastTravel;
        public override bool CanCurrentlyBeOverriden => false;
        public override bool RequirementsFulfilled => true;

        float _positionUpdateInterval;
        GameObject _followObject;
        
        protected override void Init() {
            Controller.audioAnimator.SetFloat(Movement, 0);
            Controller.audioAnimator.ResetAllTriggersAndBool();
            Controller.isSwimming = false;
            Controller.isKicking = false;
            Controller.isSlippingFromAI = false;
            
            Controller.Controller.enabled = false;
            _positionUpdateInterval = PositionUpdateInterval;
        }
        
        public void SetFollowObject(GameObject followObject) {
            _followObject = followObject;
        }

        public override void Update(float deltaTime) {
            if (_followObject == null) {
                return;
            }

            _positionUpdateInterval -= deltaTime;
            if (_positionUpdateInterval <= 0) {
                Controller.transform.position = _followObject.transform.position;
                _positionUpdateInterval = PositionUpdateInterval;
            }
        }
        
        public override void FixedUpdate(float deltaTime) { }
        protected override void SetupForceExitConditions() { }

        protected override void OnDiscard(bool fromDomainDrop) {
            _followObject = null;
            if (!fromDomainDrop) {
                Controller.Controller.enabled = true;
            }
        }
    }
}