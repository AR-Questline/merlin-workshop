using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using FMODUnity;
using Pathfinding;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    [RequireComponent(typeof(RichAI))]
    public class VCPetController : ViewComponent<Location> {
        const float GroundRaycastSafetyOffset = 0.1f;
        const float GroundRaycastDistance = 0.3f;
        
        const string MovementSpeedsGroup = "Movement Speeds";
        const string FollowingGroup = "Movement Speeds";
        const string TurningGroup = "Turning";
        const string VisualOrientation = "Visual Orientation";

        [SerializeField, BoxGroup(MovementSpeedsGroup)] float walkingSpeed;
        [SerializeField, BoxGroup(MovementSpeedsGroup)] float runningSpeed;
        [SerializeField, BoxGroup(MovementSpeedsGroup)] float sprintingSpeed;
        [Space]
        [SerializeField, BoxGroup(MovementSpeedsGroup)] float distanceToStartRunning;
        [SerializeField, BoxGroup(MovementSpeedsGroup)] float distanceToStartSprinting;

        [SerializeField, BoxGroup(FollowingGroup)] float minDistanceToReachTarget;
        [SerializeField, BoxGroup(FollowingGroup)] float minDistanceToStartFollowing;
        [SerializeField, BoxGroup(FollowingGroup)] float minDistanceToTeleportToHero;
        [SerializeField, BoxGroup(FollowingGroup)] float teleportDistanceFromTarget;
        [SerializeField, BoxGroup(FollowingGroup)] bool alwaysTeleportBehindHero;
        
        [SerializeField, BoxGroup(TurningGroup)] float turningSpeed;
        [SerializeField, BoxGroup(TurningGroup)] float minAngleToStartStationaryTurning;
        [SerializeField, BoxGroup(TurningGroup)] float minDistanceToStartStationaryTurning;
        [SerializeField, BoxGroup(TurningGroup)] float maxAngleToFaceTargetWhenFollowing;
        
        [SerializeField, BoxGroup(VisualOrientation)] Transform visualRoot;
        [SerializeField, BoxGroup(VisualOrientation)] float visualRootAdjustSpeed;
        [SerializeField, BoxGroup(VisualOrientation)] float visualRootMaxAngle;
        
        PetElement _pet;
        Transform _transform;
        ARPetAnimancer _animancer;
        RichAI _richAI;
        Seeker _seeker;
        TimeDependent _timeDependent;
        Transform _followedLocation;
        ARFmodEventEmitter _idleAudioEmitter;
        
        bool _initialized;
        bool _isFollowing;
        bool _stationaryRotateToTarget;
        bool _teleportingNearTarget;

        Vector3 _targetPosition;
        float _distanceToTarget;
        Vector2 _facingDirLastFrame;
        float _measuredAngularVelocity;
        float _visualRootMaxAngleCos;

        bool IsTaunting => _animancer.CurrentState == ARPetAnimancer.State.Taunt;
        bool IsPetting => _animancer.CurrentState == ARPetAnimancer.State.Pet;
        public Vector3 DirectionToTarget => (_targetPosition - _transform.position).normalized;
        public Vector3 WalkingVelocity => _richAI.velocity;
        public float WalkingSpeed => _richAI.velocity.magnitude;
        public float AngularVelocity => _measuredAngularVelocity;
        public bool CanInteractWith => !IsTaunting;
        
        public void Initialize() {
            if (_initialized) {
                return;
            }
            
            _pet = Target?.TryGetElement<PetElement>();

            _transform = transform;
            _richAI = GetComponent<RichAI>();
            _seeker = GetComponent<Seeker>();
            _timeDependent = Target.GetOrCreateTimeDependent();
            _animancer = GetComponentInChildren<ARPetAnimancer>();
            _animancer.OnAnimatorMoved += OnAnimatorMoved;
            
            _richAI.endReachedDistance = minDistanceToStartFollowing;
            _richAI.rotationSpeed = turningSpeed;
            _richAI.enableRotation = false;
            _initialized = true;
            
            var idleEventRef = AliveAudioType.Idle.RetrieveFrom(_pet);
            if (!idleEventRef.IsNull) {
                // _idleAudioEmitter = gameObject.AddComponent<ARFmodEventEmitter>();
                // _idleAudioEmitter.ChangeEvent(idleEventRef);
                // _idleAudioEmitter.PlayEvent = EmitterGameEvent.ObjectEnable;
                // _idleAudioEmitter.StopEvent = EmitterGameEvent.ObjectDisable;
                // if (gameObject.activeInHierarchy) {
                //     _idleAudioEmitter.Play();
                // }
            }

            _visualRootMaxAngleCos = math.cos(visualRootMaxAngle * math.TORADIANS);
        }
        
        void OnDisable() {
            if (_richAI != null) {
                _richAI.enabled = false;
                _richAI.Pause();
            }
        }

        void OnEnable() {
            if (_richAI != null) {
                _richAI.enabled = true;
                _richAI.Unpause();
            }
        }
        
        void OnAnimatorMoved(Animator animator) {
            if (Target is not { HasBeenDiscarded: false }) {
                return;
            }

            if (animator.deltaPosition.magnitude > 0.001f) {
                _richAI.Move(animator.deltaPosition);
            }
            
            if (animator.deltaRotation != Quaternion.identity) {
                float deltaAngle = Mathf.DeltaAngle(0, animator.deltaRotation.eulerAngles.y);
                _transform.rotation *= Quaternion.Euler(0, deltaAngle, 0);
            }
        }

        void Update() {
            if (!_initialized || _pet is not { HasBeenDiscarded: false } || _pet.ParentModel.Interactability != LocationInteractability.Active) {
                return;
            }
            
            float deltaTime = _timeDependent.DeltaTime;

            if (IsTaunting) {
                _richAI.maxSpeed = 0f;
                _richAI.enableRotation = false;
            } else {
                RecalculateTarget();
                RefreshMaxSpeed();
                UpdateFollowing();
                UpdateRotation(deltaTime);
                CheckForTeleport();
            }

            AdjustVisualRootToGround(deltaTime);
            MeasureAngularVelocity(deltaTime);
            
            _animancer.UpdateToPet(this, deltaTime);
        }

        void RecalculateTarget() {
            var petPosition = _transform.position;
            
            if (_pet.TargetToFollow == null) {
                _targetPosition = petPosition;
                _distanceToTarget = 0f;
            } else {
                _targetPosition = _pet.TargetToFollow.Coords;
                _distanceToTarget = Vector3.Distance(petPosition, _targetPosition);
            }

            if (_isFollowing) {
                _richAI.destination = _targetPosition;
            }
        }

        void RefreshMaxSpeed() {
            if (_distanceToTarget > distanceToStartSprinting) {
                _richAI.maxSpeed = sprintingSpeed;
            } else if (_distanceToTarget > distanceToStartRunning) {
                _richAI.maxSpeed = runningSpeed;
            } else {
                _richAI.maxSpeed = walkingSpeed;
            }
        }

        void UpdateFollowing() {
            if (!_isFollowing && _pet.ShouldFollowTarget && _distanceToTarget > minDistanceToStartFollowing) {
                _isFollowing = true;
                _richAI.enableRotation = true;
                _richAI.endReachedDistance = minDistanceToReachTarget;
            } else if (_isFollowing && (_richAI.reachedDestination || !_pet.ShouldFollowTarget)) {
                _isFollowing = false;
                _richAI.enableRotation = false;
                _richAI.endReachedDistance = minDistanceToStartFollowing;
            }
        }

        void UpdateRotation(float deltaTime) {
            Vector3 lookDirection = DirectionToTarget.ToHorizontal3().normalized;
            var angleToTarget = Vector2.Angle(_transform.forward.ToHorizontal2(), lookDirection.ToHorizontal2());
            
            bool rotateToTarget = _isFollowing && angleToTarget < maxAngleToFaceTargetWhenFollowing;

            if (!_isFollowing) {
                if (angleToTarget > minAngleToStartStationaryTurning && _distanceToTarget < minDistanceToStartStationaryTurning) {
                    _stationaryRotateToTarget = true;
                }
                if (_stationaryRotateToTarget && angleToTarget <= 0.01f) {
                    _stationaryRotateToTarget = false;
                }
                if (_stationaryRotateToTarget && !IsPetting) {
                    rotateToTarget = true;
                }
            }

            if (rotateToTarget) {
                var targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation, turningSpeed * deltaTime);
            }
        }

        void CheckForTeleport() {
            if (_teleportingNearTarget && _seeker.IsDone()) {
                _teleportingNearTarget = false;
            }
            
            if (_isFollowing && _distanceToTarget > minDistanceToTeleportToHero && !_teleportingNearTarget) {
                TryTeleportNearTarget();
            }
        }

        void AdjustVisualRootToGround(float deltaTime) {
            RaycastHit hit;
            
            var groundNormal = Vector3.up;

            var groundRaycastStart = _transform.position + Vector3.up * GroundRaycastSafetyOffset;
            if (Physics.Raycast(groundRaycastStart, Vector3.down, out hit, GroundRaycastDistance, _richAI.groundMask)) {
                groundNormal = hit.normal;
            }

            if (groundNormal.y < _visualRootMaxAngleCos) {
                groundNormal = Vector3.up;
            }
            
            var targetVisualRotation = Quaternion.FromToRotation(Vector3.up, groundNormal) * _transform.rotation;
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetVisualRotation, deltaTime * visualRootAdjustSpeed);
        }
        
        void MeasureAngularVelocity(float deltaTime) {
            if (deltaTime > 0) {
                var currentFacingDir = _transform.forward.ToHorizontal2();
                _measuredAngularVelocity = Vector2.SignedAngle(currentFacingDir, _facingDirLastFrame) / deltaTime;
                _facingDirLastFrame = currentFacingDir;
            }
        }

        public void TryTeleportNearTarget() {
            if (_distanceToTarget == 0f || _pet.TargetToFollow == null) {
                return;
            }
            
            Vector3 targetPosition = _targetPosition;
            Vector3 teleportDesiredDirection = DirectionToTarget * -1f;
            if (alwaysTeleportBehindHero && _pet.TargetToFollow is Hero hero) {
                teleportDesiredDirection = hero.Forward() * -1f;
            };
            Vector3 teleportDesiredPosition = targetPosition + teleportDesiredDirection * teleportDistanceFromTarget;

            _teleportingNearTarget = true;
            _richAI.Pause();
            _seeker.StartPath(targetPosition, teleportDesiredPosition, path => {
                var positionToTeleportTo = path.error ? _targetPosition : path.vectorPath[^1];
                NNInfo nnInfo = AstarPath.active.GetNearest(positionToTeleportTo);
                Teleport(nnInfo.node != null ? nnInfo.position : positionToTeleportTo);
                _richAI.Unpause();
                _teleportingNearTarget = false;
            });
        }
        
        public void Teleport(Vector3 position) {
            if (Target.TryGetElement(out GameplayUniqueLocation gameplayUniqueLocation)) {
                gameplayUniqueLocation.TeleportIntoCurrentScene(position);
            } else {
                Target.SafelyMoveTo(position);
            }

            _targetPosition = position;
            _richAI.destination = position;
            _richAI.Teleport(position);
        }

        public bool IsMoving() {
            return !IsTaunting && (WalkingSpeed >= 0.1f || math.abs(AngularVelocity) >= 0.01f);
        }
        
        public void StartTaunt() {
            _animancer.PlayTauntAnimation();
        }

        public void StartPet() {
            _animancer.PlayPetAnimation();
        }
        
        protected override void OnDestroy() {
            if (_idleAudioEmitter != null) {
                Destroy(_idleAudioEmitter);
                _idleAudioEmitter = null;
            }
            
            _animancer.UnloadAnimations();
            base.OnDestroy();
        }
    }
}