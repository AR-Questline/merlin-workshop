using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours {
    [Serializable]
    public partial class CircleAroundTargetBehaviour : KeepPositionBehaviour {
        const float CirclePointAcceptRange = 1.5f;
        
        [SerializeField] float leaveCircleCooldown;
        [SerializeField] int minimumPointsReachedToLeave = 2;
        [SerializeField] float maxOffset = 3;
        [SerializeField] float changeDirectionChance = 0.2f;
        [SerializeField] int minimumPointsToChangeDirection = 5;

        CircleAroundTargetService _service;
        int _leavePointsReached, _changeDirectionPointsReached;
        
        public int CurrentIndex { get; set; }
        public int CurrentVersion { get; set; }
        public float Offset { get; private set; }
        public bool AscendingDirection { get; private set; }
        public float LeaveCircleCooldown => leaveCircleCooldown;
        
        CircleAroundTargetService Service => _service ??= World.Services.Get<CircleAroundTargetService>();

        protected override bool StartBehaviour() {
            _leavePointsReached = 0;
            _changeDirectionPointsReached = 0;
            ChangeDirection(true);
            return base.StartBehaviour();
        }
        
        public override void Update(float deltaTime) {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            if (target == null) {
                ParentModel.StartWaitBehaviour();
                return;
            }
            
            _inStateDuration += deltaTime;
            
            if (ParentModel.NpcMovement.CurrentState != _keepPosition) {
                ParentModel.NpcMovement.ChangeMainState(_keepPosition);
            }

            if (InCorrectPosition) {
                if (_leavePointsReached >= minimumPointsReachedToLeave && Service.CanLeaveCircling(this, target) && ParentModel.TryToStartNewBehaviourExcept(this)) {
                    _leavePointsReached = 0;
                    Service.LeftCircling(this, target);
                    return;
                }
                GoToNextPosition(target);
            }
        }

        public void ChangeDirection(bool newDirection) {
            _changeDirectionPointsReached = 0;
            AscendingDirection = newDirection;
            if (maxOffset > 0) {
                Offset = RandomUtil.UniformFloat(0f, maxOffset);
            }
        }

        protected override CharacterPlace GetTargetPosition() {
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            if (target == null) {
                ParentModel.StartWaitBehaviour();
                return new CharacterPlace(ParentModel.Coords, CirclePointAcceptRange);
            }
            var position = Service.GetClosestCirclingPoint(this, target);
            return new CharacterPlace(position, CirclePointAcceptRange);
        }

        void GoToNextPosition(ICharacter target) {
            _leavePointsReached++;
            _changeDirectionPointsReached++;
            if (_changeDirectionPointsReached >= minimumPointsToChangeDirection && RandomUtil.WithProbability(changeDirectionChance)) {
                ChangeDirection(!AscendingDirection);
            }
            _targetPosition = GetNextTargetPosition(target);
            _keepPosition.UpdatePlace(_targetPosition);
        }
        
        CharacterPlace GetNextTargetPosition(ICharacter target) {
            if (target == null) {
                ParentModel.StartWaitBehaviour();
                return new CharacterPlace(ParentModel.Coords, CirclePointAcceptRange);
            }
            var position = Service.GetNextCirclingPoint(this, target);
            return new CharacterPlace(position, CirclePointAcceptRange);
        }
    }
}
