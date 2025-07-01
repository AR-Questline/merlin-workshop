using Awaken.TG.Main.Fights.Finishers;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class FinisherMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.FinisherMovement;

        FinisherData.RuntimeData _data;
        Vector3 _initialPosition;
        HeroCamera _heroCamera;
        float _lerpMovePercentage;
        
        public override MovementType Type => MovementType.Finisher;
        public override bool CanCurrentlyBeOverriden => false;
        public override bool RequirementsFulfilled => true;
        
        protected override void Init() {
            _heroCamera = Controller.HeroCamera;
            Controller.ToggleCrouch(0.25f, false);
            Controller.Controller.SimpleMove(Vector3.zero);
            Controller.Controller.enabled = false;
        }

        protected override void SetupForceExitConditions() { }

        public void Setup(FinisherData.RuntimeData data) {
            _data = data;
            _heroCamera.ActivateFinisherCamera();
            _initialPosition = Hero.Coords;
            _lerpMovePercentage = 0f;
            UIStateStack.Instance.PushState(UIState.BlockInput, this);
        }

        public override void Update(float deltaTime) {
            if (_lerpMovePercentage < 1f) {
                _lerpMovePercentage += deltaTime * _data.heroMoveDeltaTimeMultiplier;
                Controller.Transform.position = Vector3.Lerp(_initialPosition, _data.heroFinalPosition, _data.heroMoveEasingType.Calculate(_lerpMovePercentage));
            }
            Controller.Transform.forward = _data.heroLookAtPosition - Controller.Transform.position;
        }

        public override void FixedUpdate(float deltaTime) { }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (Controller == null) {
                // Already discarded or not initialized. this means that the layer is back to default
                return;
            }
            _heroCamera.DeactivateFinisherCamera();
            Hero.MoveTo(Ground.SnapToGround(Controller.Transform.position + Vector3.up * 0.15f, findClosest: true) + Vector3.up * 0.05f);
            Controller.Controller.enabled = true;
            Controller.Controller.SimpleMove(Vector3.zero);
            Controller.SetVerticalVelocity(0);
            UIStateStack.Instance.ReleaseAllOwnedBy(this);
            base.OnDiscard(fromDomainDrop);
        }
    }
}