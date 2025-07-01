using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.Animations;
using Awaken.TG.Main.AI.Idle.Interactions.Patrols;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class DialogueNavmeshBasedMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.DialogueNavmeshBasedMovement;

        const float RotationSpeedStart = 0.5f;
        const float LookAtPathDistance = 6f;

        const float MoveDirYReachedThreshold = 0.85f; // Don't try to reach points that are too high or too low (most likely collider vs navmesh innacuracy)
        const float MoveDirYCameraPitchThreshold = 0.3f; // Don't move camera pitch too much
        const float MinWaypointReachDistance = 0.25f;
        const float MinWaypointReachSqrDistance = MinWaypointReachDistance * MinWaypointReachDistance;
        static readonly int Movement = Animator.StringToHash("Movement");
        
        // Data
        CharacterPlace _targetPlace;
        bool _rotateTowardsMovement;
        // Path
        List<Vector3> _calculatedPath;
        bool _pathCalculated;
        int _currentIndex;

        HeroCamera _heroCamera;
        [UnityEngine.Scripting.Preserve] bool _dialogueCameraActivityCache;

        public override MovementType Type => MovementType.DialogueNavmeshBased;
        public override bool CanCurrentlyBeOverriden => true;
        public override bool RequirementsFulfilled => true;

        public override void Update(float deltaTime) {
            if (_pathCalculated) {
                Move(deltaTime);
            }
        }
        public override void FixedUpdate(float deltaTime) { }

        protected override void Init() {
            Controller.audioAnimator.ResetAllTriggersAndBool();
            Controller.Grounded = true;
            Controller.HeroCamera.SetPitch(0);

            _heroCamera = Controller.HeroCamera;
            _dialogueCameraActivityCache = Controller.dialogueVirtualCamera.enabled;
        }
        protected override void SetupForceExitConditions() { }
        
        public void SetABPath(CharacterPlace targetPlace, bool rotateTowardsMovement) {
            _pathCalculated = false;
            _targetPlace = targetPlace;
            _rotateTowardsMovement = rotateTowardsMovement;
            if (rotateTowardsMovement) {
                _heroCamera.LockActiveDialogueCamera(false);
            }
            
            var from = ParentModel.Coords;
            from.y = Ground.HeightAt(from, findClosest: true);
            var to = targetPlace.Position;
            to.y = Ground.HeightAt(targetPlace.Position, findClosest: true);
            
            SetABPathInternal(from, to).Forget();
        }

        async UniTaskVoid SetABPathInternal(Vector3 from, Vector3 to) {
            if (!await AsyncUtil.DelayFrame(this, 2)) {
                // Frame delay is necessary for funnel to register properly for async path calculation
                return;
            }
            var path = await CalculateABPath(from, to);
            if (path == null || path.CompleteState is PathCompleteState.Error or PathCompleteState.NotCalculated) {
                PathCalculationFailed(path);
                return;
            }
            _calculatedPath = path.vectorPath;
            
            _currentIndex = 1;
            _pathCalculated = true;
        }
        
        public void SetPatrolPath(PatrolPath patrol, bool moveFromCurrentHeroPosition, bool rotateTowardsMovement) {
            _pathCalculated = false;
            _targetPlace = new CharacterPlace(patrol.waypoints.Last().position, MinWaypointReachDistance);
            _rotateTowardsMovement = rotateTowardsMovement;
            if (rotateTowardsMovement) {
                _heroCamera.LockActiveDialogueCamera(false);
            }
            SetPatrolPathInternal(patrol, moveFromCurrentHeroPosition).Forget();
        }

        async UniTaskVoid SetPatrolPathInternal(PatrolPath patrol, bool moveFromCurrentHeroPosition) {
            if (!await AsyncUtil.DelayFrame(this)) {
                // Frame delay is necessary for funnel to register properly for async path calculation
                return;
            }
            if (moveFromCurrentHeroPosition) {
                var path = await CalculateABPath(ParentModel.Coords, patrol.waypoints[0].position);
                if (path.CompleteState is PathCompleteState.Error or PathCompleteState.NotCalculated) {
                    PathCalculationFailed(path);
                    return;
                }
                _calculatedPath = path.vectorPath;
            } else {
                _calculatedPath = new List<Vector3>();
            }
            
            for (int i = 1; i < patrol.waypoints.Length; i++) {
                var path = await CalculateABPath(patrol.waypoints[i - 1].position, patrol.waypoints[i].position);
                if (path.CompleteState is PathCompleteState.Error or PathCompleteState.NotCalculated) {
                    PathCalculationFailed(path);
                    return;
                }
                var newPoints = path.vectorPath;
                if (i < patrol.waypoints.Length - 1) {
                    // First and Last points from "adjacent paths" are duplicates.
                    newPoints.RemoveAt(newPoints.Count - 1);
                }
                _calculatedPath.AddRange(newPoints);
            }
            _currentIndex = 1;
            _pathCalculated = true;
        }
        
        async UniTask<Path> CalculateABPath(Vector3 from, Vector3 to) {
            Path path = ABPath.Construct(from, to);
            AstarPath.StartPath(path);
            if (!await AsyncUtil.WaitUntil(this, () => path.CompleteState != PathCompleteState.NotCalculated)) {
                return null;
            }
            new StartEndModifier().Apply(path);
            PathfindingUtils.SimulateFunnelModifier(ref path);
            return path;
        }

        void Move(float deltaTime) {
            var positionCache = ParentModel.Coords;
            if (_targetPlace.Contains(positionCache)) {
                ParentModel.ReturnToDefaultMovement();
                return;
            }

            Vector3 moveDir;
            Vector3 moveDirNormalized;
            float sqrDistance;
            while (true) {
                if (_currentIndex >= _calculatedPath.Count) {
                    ParentModel.ReturnToDefaultMovement();
                    return;
                }
                
                moveDir = _calculatedPath[_currentIndex] - ParentModel.Coords;
                sqrDistance = moveDir.sqrMagnitude;
                
                if (sqrDistance <= MinWaypointReachSqrDistance) {
                    SetNextIndex();
                    continue;
                }
                
                moveDirNormalized = moveDir.normalized;
                if (moveDirNormalized.y is < -MoveDirYReachedThreshold or > MoveDirYReachedThreshold) {
                    SetNextIndex();
                    continue;
                }

                break;
            }

            Vector3 moveVector = moveDirNormalized;
            if (moveVector.y < -MoveDirYCameraPitchThreshold) {
                moveVector.y = -MoveDirYCameraPitchThreshold;
            } else if (moveVector.y > MoveDirYCameraPitchThreshold) {
                moveVector.y = MoveDirYCameraPitchThreshold;
            }
            moveVector *= (Controller.Data.walkSpeed * deltaTime);
            
            if (moveVector.sqrMagnitude > sqrDistance) {
                // We would overshoot the point, better to move directly to the point
                moveVector = moveDir;
                Move(moveVector, deltaTime);
                SetNextIndex();
                return;
            }
            
            Move(moveVector, deltaTime);
            
            var moveVectorSqr = (positionCache - ParentModel.Coords).sqrMagnitude;
            var newSqrDistance = (_calculatedPath[_currentIndex] - ParentModel.Coords).sqrMagnitude;
            if (moveVectorSqr < 0.0000001f || newSqrDistance >= sqrDistance) {
                // Check if we are stuck or we move away from the point
                SetNextIndex();
            }
        }

        void SetNextIndex() {
            _currentIndex++;
        }
        
        void Move(Vector3 moveVector, float deltaTime) {
            Controller.audioAnimator.SetFloat(Movement, Controller.Data.walkSpeed);
            moveVector.y += (Controller.Data.gravity * deltaTime);
            Controller.PerformMoveStep(moveVector);
            Controller.ApplyTransformToTarget();
            if (_rotateTowardsMovement && _currentIndex < _calculatedPath.Count) {
                var lookPos = GetPositionAfterDistance(_currentIndex, Hero.Coords, LookAtPathDistance);
                var lookDir = lookPos - Hero.Coords;
                lookDir = lookDir.normalized;
                lookDir.y = lookDir.y switch {
                    > MoveDirYCameraPitchThreshold => MoveDirYCameraPitchThreshold,
                    < -MoveDirYCameraPitchThreshold => -MoveDirYCameraPitchThreshold,
                    _ => lookDir.y
                };
                _heroCamera.FollowRotation(Quaternion.LookRotation(lookDir).eulerAngles, deltaTime, RotationSpeedStart);
            }
        }

        Vector3 GetPositionAfterDistance(int currentTargetIndex, Vector3 currentPos, float targetDistance) {
            var currentPoint = _calculatedPath[currentTargetIndex];
            var direction = currentPoint - currentPos;
            float combinedLength = direction.magnitude;
            if (combinedLength > targetDistance) {
                return currentPos + direction.normalized * targetDistance;
            }
            
            while (currentTargetIndex + 1 < _calculatedPath.Count) {
                currentTargetIndex++;
                direction = _calculatedPath[currentTargetIndex] - currentPoint;
                float magnitude = direction.magnitude;
                if (combinedLength + magnitude > targetDistance) {
                    currentPoint += direction.normalized * (targetDistance - combinedLength);
                    break;
                }
                currentPoint = _calculatedPath[currentTargetIndex];
                combinedLength += magnitude;
            }
            
            return currentPoint;
        }

        void PathCalculationFailed(Path path) {
            if (path == null) {
                Log.Important?.Error($"Path calculation for Hero status aborted");
            } else {
                Log.Critical?.Error($"Path calculation for Hero status {path.CompleteState}: {path.errorLog}");
            }

            ParentModel.ReturnToDefaultMovement();
            if (!HasBeenDiscarded) {
                Discard();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop && Controller.audioAnimator != null) {
                Controller.audioAnimator.SetFloat(Movement, 0);
            }
            _heroCamera.UnlockActiveDialogueCamera();
            base.OnDiscard(fromDomainDrop);
        }
    }
}