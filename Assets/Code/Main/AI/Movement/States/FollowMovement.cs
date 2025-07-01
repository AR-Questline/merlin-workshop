using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Grounds;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class FollowMovement : MovementState {
        const float WaitTime = 3f;
        const float StandNextToDistance = 3;
        
        public IGrounded Target { get; }
        
        public Vector3 StartPoint { get; }
        float? MaxDistanceSqr { get; set; }
        float DistanceForTrotSqr { get; }

        bool _isAtMaxDistance;
        bool _isAtTrotDistance;
        bool _isStanding;
        CharacterPlace _lastDestination;
        CharacterPlace _destination;
        RotateTowardsMovement _rotateTowardsMovement;
        RotateTowardsCustomTarget _rotateTowardsCustomTarget;
        
        float? _waitTime;
        
        bool ShouldStartWaiting => _isAtMaxDistance && _waitTime == null;
        bool WaitingToReturn => _waitTime is > 0;
        bool ShouldStartReturning => _waitTime is <= 0 and not -1;
        
        public float? WaitTimeLeft => _waitTime;
        public float DistanceFromStart => Npc.Coords.DistanceTo(StartPoint);

        public FollowMovement(IGrounded target, float distanceForTrot,  Vector3 startPoint, float? maxDistance = null) {
            Target = target;
            StartPoint = startPoint;
            
            DistanceForTrotSqr = math.square(distanceForTrot);
            if (maxDistance != null) {
                MaxDistanceSqr = math.square(maxDistance.Value);
            }
            
            _rotateTowardsMovement = new RotateTowardsMovement();
            _rotateTowardsCustomTarget = new RotateTowardsCustomTarget(target);
        }
        
        [UnityEngine.Scripting.Preserve]
        public void LimitDistanceFromStart(float maxDistance) {
            MaxDistanceSqr = math.square(maxDistance);
        }
        
        [UnityEngine.Scripting.Preserve]
        public void ClearDistanceLimit() {
            MaxDistanceSqr = null;
        }
        
        public override VelocityScheme VelocityScheme => _isAtTrotDistance ? VelocityScheme.Trot : VelocityScheme.Walk;

        protected override void OnEnter() { }
        protected override void OnExit() { }

        protected override void OnUpdate(float deltaTime) {
            _isAtMaxDistance = Npc.Coords.SquaredDistanceTo(StartPoint) > MaxDistanceSqr;
            _isAtTrotDistance = Target.Coords.SquaredDistanceTo(Npc.Coords) > DistanceForTrotSqr;

            Controller.SetRotationScheme(_isAtTrotDistance ? _rotateTowardsMovement : _rotateTowardsCustomTarget, VelocityScheme);
            
            _lastDestination = _destination;

            if (Waiting(deltaTime)) return;
            _waitTime = null;

            if (!_isStanding && _destination.Contains(Npc.Coords)) {
                // If we're standing next to target, don't move
                _destination = new CharacterPlace(Npc, StandNextToDistance);
                _isStanding = true;
            } else if (_isStanding) {
                // While we're standing next to target check if we need to move closer
                // to prevent NPC spazing out we have a tolerance for the player before we start following again
                var targetDestination = new CharacterPlace(Target, StandNextToDistance * 1.5f);
                if (!targetDestination.Contains(Npc.Coords) || !Npc.Element<NpcCrimeReactions>().IsSeeingHero) {
                    _destination = targetDestination;
                    _isStanding = false;
                    return;
                }
            } else {
                // While we're moving towards target assume target has moved, update destination
                _destination = new CharacterPlace(Target, StandNextToDistance);
            }

            TrySetDestination(_destination);
        }

        bool Waiting(float deltaTime) {
            if (ShouldStartWaiting) {
                _destination = new CharacterPlace(Npc, StandNextToDistance);
                TrySetDestination(_destination);

                _waitTime = WaitTime;
                return true;
            }

            if (ShouldStartReturning) {
                End();
                _waitTime = null;
                return true;
            }

            if (WaitingToReturn) {
                _waitTime -= deltaTime;
                return true;
            }

            return false;
        }

        bool TrySetDestination(CharacterPlace destination, bool force = false) {
            if (!force && _lastDestination.ApproximatelyEqual(destination, 2)) {
                // Cancel setting for lazy updates
                _destination = _lastDestination;
                return false;
            }
            return Controller.TrySetDestination(destination.Position);
        }

        [UnityEngine.Scripting.Preserve]
        void StartReturning() {
            _waitTime = -1;
        }
    }
}