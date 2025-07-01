using System;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Animations;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MeleeBehaviours {
    [Serializable]
    public partial class ChargeThroughBehaviour : AttackBehaviour {
        [SerializeField] float chargeDistanceOverflow = 15f;
        [SerializeField] float stopRecalculatingPathAfterTime = 0.5f;
        [SerializeField] NpcStateType animatorPrepareStateType = NpcStateType.LongRange;
        [SerializeField] NpcStateType animatorStateType = NpcStateType.Movement;
        [SerializeField] float chargeSpeed = 25f;
        [SerializeField] AnimationCurve chargeSpeedCurve;
        [SerializeField] ARAnimationEvent animationEvent;
        
        RaycastCheck _raycastCheck;
        IHandOwner<ICharacter> _handOwner;
        CharacterPlace _destination;
        float _inStateDuration;
        bool _preparingCharge;
        
        public override bool CanBeUsed => true;
        protected override NpcStateType StateType => _preparingCharge ? animatorPrepareStateType : animatorStateType;
        protected override MovementState OverrideMovementState => new NoMove();
        
        protected override bool OnStart() {
            _handOwner ??= ParentModel.NpcElement.GetHandOwner();
            _raycastCheck = new RaycastCheck {
                prevent = GameConstants.Get.obstaclesMask
            };
            
            ParentModel.NpcMovement.ChangeMainState(new NoMove());
            ParentModel.NpcMovement.Controller.ToggleGlobalRichAIActivity(false);
            
            _inStateDuration = 0f;
            _preparingCharge = true;
            return true;
        }

        protected override void AfterStart() {
            _handOwner.OnAttackRelease(animationEvent.CreateData());
        }

        public override void OnStop() {
            ParentModel.NpcMovement.Controller.ToggleGlobalRichAIActivity(true);
            _handOwner.OnAttackRecovery(animationEvent.CreateData());
        }

        void CalculateDestination() {
            var targetCoords = ParentModel.NpcElement.GetCurrentTarget().Coords;
            var direction = (targetCoords - ParentModel.Coords).normalized;
            _destination = new CharacterPlace(targetCoords + direction * chargeDistanceOverflow, KeepPositionBehaviour.TargetPositionAcceptRange);
            ParentModel.NpcMovement.Controller.SetForwardInstant(direction.ToVector2());
        }

        void End() {
            ParentModel.StartWaitBehaviour();
        }

        protected override void OnAnimatorExitDesiredState() {
            ParentModel.SetAnimatorState(animatorStateType, overrideCrossFadeTime: OverrideCrossFadeTime);
            _preparingCharge = false;
        }
        
        public override void OnUpdate(float deltaTime) {
            if (_preparingCharge || _inStateDuration < stopRecalculatingPathAfterTime) {
                CalculateDestination();
            }
            
            if (_preparingCharge || deltaTime <= 0) {
                return;
            }
            
            if (_destination.Contains(ParentModel.Coords)) {
                End();
                return;
            }

            _inStateDuration += deltaTime;
            
            var from = ParentModel.Coords;
            var curveSpeed = chargeSpeedCurve.Evaluate(_inStateDuration);
            var distance = chargeSpeed * deltaTime * curveSpeed;
            var to = Vector3.MoveTowards(ParentModel.Coords, _destination.Position, distance);
            
            if (curveSpeed <= 0.01f) {
                ParentModel.ParentModel.SafelyMoveTo(to);
                return;
            }
            
            if (_raycastCheck.Raycast(from + Vector3.up, to - from, distance, 0).Prevented) {
                End();
                return;
            }
            
            var coordsBefore = ParentModel.Coords;
            ParentModel.ParentModel.SafelyMoveTo(to);
            if (coordsBefore == ParentModel.Coords) {
                End();
            }
        }
    }
}
