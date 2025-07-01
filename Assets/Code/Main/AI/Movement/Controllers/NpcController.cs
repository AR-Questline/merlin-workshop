using System;
using System.Collections.Generic;
using Awaken.TG.Graphics.Animations;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Movement.Controllers {
    [RequireComponent(typeof(RichAI))]
    public class NpcController : ViewComponent<Location>, IIsGroundedProvider, IOverlapRecoveryProvider {
        public const float DefaultWalkSpeed = 2f, DefaultTrotSpeed = 4f, DefaultRunSpeed = 6f;
        public const float DefaultWalkBackwardsSpeed = 1f, DefaultTrotBackwardsSpeed = 2f, DefaultRunBackwardsSpeed = 3f;
        const float DefaultSlowDownTime = 0.33f;
        const float CombatSlowDownTime = 0.05f;
        const float DamageNullify = 5f;
        const float MaxRunningIntoWallDuration = 15f;
        const float MinRunningIntoWallDuration = 2.5f;
        const float RunningIntoWallProlong = 1.5f;
        
        // === Fields

        // -- Inspector
        public float exitDurationFromAttackAnimations = 0.3f;
        public bool forceAllowOverlapRecovery;

        [SerializeField, FoldoutGroup("Movement Speed")] bool overrideMovementSpeed;
        [SerializeField, FoldoutGroup("Movement Speed"), ShowIf(nameof(overrideMovementSpeed)), Range(0, 25)] 
        float walkSpeed = DefaultWalkSpeed;
        [SerializeField, FoldoutGroup("Movement Speed"), ShowIf(nameof(overrideMovementSpeed)), Range(0, 25)] 
        float trotSpeed = DefaultTrotSpeed;
        [SerializeField, FoldoutGroup("Movement Speed"), ShowIf(nameof(overrideMovementSpeed)), Range(0, 25)] 
        float runSpeed = DefaultRunSpeed;
        [SerializeField, FoldoutGroup("Movement Speed"), ShowIf(nameof(overrideMovementSpeed)), Range(0, 25)] 
        float backwardsWalkSpeed = DefaultWalkBackwardsSpeed;
        [SerializeField, FoldoutGroup("Movement Speed"), ShowIf(nameof(overrideMovementSpeed)), Range(0, 25)] 
        float backwardsTrotSpeed = DefaultTrotBackwardsSpeed;
        [SerializeField, FoldoutGroup("Movement Speed"), ShowIf(nameof(overrideMovementSpeed)), Range(0, 25)]
        float backwardsRunSpeed = DefaultRunBackwardsSpeed;
        
        [SerializeField] bool forwardMovementOnly;
        [SerializeField, HideIf(nameof(forwardMovementOnly))] bool updateAnimatorOnlyVertical;
        [SerializeField] float minDistanceToHero = 2f;
        [SerializeField] bool canOverrideMinDistanceToHero = true;
        [SerializeField] float angularSpeed = 180, combatAngularSpeed = 270;
        [SerializeField] float trackingVisualFallbackForce = 5.0f;
        [SerializeField] float accelerationSpeed = 2.5f, decelerationSpeed = 1.1f;
        [SerializeField] public CharacterGroundedData data;
        
        // -- Variables
        public Action onReached;
        DelayedAngle _rotation;
        float _rootMotionTrackingOffset;
        HashSet<NpcStateType> _targetRootRotationActiveStates = new();
        float _timeSinceNoRootRotation;

        bool _initialized;
        bool _grounded = true;
        bool _teleport;
        bool _forcedForwardMovementOnly;
        bool _destinationReached;
        bool _runningIntoWall;
        bool _movingToAbyss;
        bool _hasInCombatParameter;
        bool _globalRichAIEnabled = true;
        bool _idleRichAIEnabled = true;
        float _runningIntoWallDuration;
        float _runningIntoWallProlong;
        TeleportContext _teleportContext;
        TeleportDestination _teleportDestination;
        RVOLayer _originalRvoCollisionLayer;

        Vector3 _slopeDir;
        Vector3 _hitNormal;
        Vector3 _rootBoneOffset;
        float _yLiftOff;
        float _fallDamageDisableEndFixedUpdateTime;
        int _fallDamageDisableEndFrame;

        MovingPlatform _movingPlatform;

        Vector3 _rvoPositionOnLastMove;
        Vector3 _accumulatedVelocitySinceLastRvoUpdate;

        public float OriginalMinDistanceToTarget => minDistanceToHero;
        public bool CanOverrideMinDistanceToHero => canOverrideMinDistanceToHero;
        float MinDistanceToTarget => DistancesToTargetHandler.MinDistanceToTarget(Npc);
        Vector3 PhysicsBasedPosition => Npc.IsInRagdoll ? RootBone.position - _rootBoneOffset : Target.Coords;
        float YPosition => RootBone.position.y - _rootBoneOffset.y;
        bool TargetRootRotationInProgress => _targetRootRotationActiveStates.Count > 0;
        public float RootRotationTrackingOffset => _rootMotionTrackingOffset;
        public float TargetRotationDelta => Vector2.SignedAngle(LogicalForward, SteeringDirection) * -1.0f;
        public bool DisableOverlapRecovery => !forceAllowOverlapRecovery &&
                                              Npc.NpcAI is not { InCombat: true } &&
                                              Npc.Movement is not { CurrentState: PushedMovement } &&
                                              (_runningIntoWallProlong > 0 || Npc.ParentModel.GetCurrentBand() <= 0);

        // -- References
        public RichAI RichAI { get; private set; }
        public RVOController RvoController { get; private set; }
        public Seeker Seeker { get; private set; }
        public Animator Animator { get; private set; }
        public ARNpcAnimancer ARNpcAnimancer { get; private set; }
        public RootMotions.RootMotion RootMotion { get; private set; }

        public IRotationScheme RotationScheme { get; private set; }
        
        static readonly int InCombat = Animator.StringToHash("InCombat");

        // === Properties
        public bool IsGrounded => IsGroundedInternal();
        public Transform RootBone { get; private set; }
        public GameObject AlivePrefab { get; private set; }
        public ARFmodEventEmitter IdleAudioEmitter { get; private set; }

        public bool Grounded {
            get => _grounded;
            private set {
                if (_grounded && !value) {
                    _yLiftOff = YPosition;
                }
                if (!_grounded && value) {
                    OnFell();
                }
                _grounded = value;
            }
        }

        public bool ForwardMovementOnly => forwardMovementOnly || _forcedForwardMovementOnly;
        public bool UpdateAnimatorOnlyVertical => updateAnimatorOnlyVertical;
        bool RichAIEnabled => _globalRichAIEnabled && (Npc is { HasBeenDiscarded: false, NpcAI: { InIdle: false } } || _idleRichAIEnabled);

        NpcElement _element;
        NpcMovement _movement;
        public NpcElement Npc {
            get {
                bool notNull = _element != null;
                if (notNull && _element.HasBeenDiscarded) {
                    return null;
                }
                return notNull ? _element : _element = Target?.TryGetElement<NpcElement>();
            }
        }
        public NpcMovement Movement {
            get {
                bool notNull = _movement != null;
                if (notNull && _movement.HasBeenDiscarded) {
                    return null;
                }
                return notNull ? _movement : _movement = Npc?.TryGetElement<NpcMovement>();
            }
        }

        public float CurrentMaxSpeed {
            get {
                float velocitySchemeSpeed = Movement?.CurrentState?.VelocityScheme?.Speed(this) ?? DefaultRunSpeed;
                return velocitySchemeSpeed;
            }
        }

        public float WalkSpeed => overrideMovementSpeed ? walkSpeed : DefaultWalkSpeed;
        public float BackwardsWalkSpeed => overrideMovementSpeed ? backwardsWalkSpeed : DefaultWalkBackwardsSpeed;
        public float TrotSpeed => overrideMovementSpeed ? trotSpeed : DefaultTrotSpeed;
        public float BackwardsTrotSpeed => overrideMovementSpeed ? backwardsTrotSpeed : DefaultTrotBackwardsSpeed;
        public float RunSpeed => overrideMovementSpeed ? runSpeed : DefaultRunSpeed;
        public float BackwardsRunSpeed => overrideMovementSpeed ? backwardsRunSpeed : DefaultRunBackwardsSpeed;

        // === Functionality
        protected override void OnAttach() {
            Target.ListenTo(MovingPlatform.Events.MovingPlatformAdded, OnMovingPlatformAdded, this);
            Target.ListenTo(MovingPlatform.Events.MovingPlatformDiscarded, OnMovingPlatformDiscarded, this);
            Target.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, OnDistanceBandChanged, this);
        }

        public void Init() {
            Vector3 forward = transform.forward;
            _rotation.SetInstant(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
            
            RichAI = GetComponent<RichAI>();
            InitRichAIData(RichAI);
            RvoController = GetComponent<RVOController>();
            _originalRvoCollisionLayer = RvoController.collidesWith;
            Seeker = GetComponent<Seeker>();
            RootMotion = GetComponentInChildren<RootMotions.RootMotion>();
            Animator = GetComponentInChildren<Animator>();
            ARNpcAnimancer = Animator.GetComponent<ARNpcAnimancer>();
            _hasInCombatParameter = Animator.HasParameter(InCombat);
            
            RootBone = gameObject.FindChildWithTagRecursively("RootBone", includeDisabled: true);
            if (RootBone == null) {
                Log.Important?.Error($"There is no bone with RootBone tag set!!! This will lead to many errors!!! Prefab: {gameObject.name}", gameObject);
            }
            _rootBoneOffset = new Vector3(0, (RootBone.position - transform.position).y, 0);
            
            Transform alivePrefabTransform = gameObject.FindChildWithTagOrNameRecursively("AlivePrefab", "AlivePrefab", includeDisabled: true);
            if (alivePrefabTransform != null) {
                AlivePrefab = alivePrefabTransform.gameObject;
                var idleEventRef = AliveAudioType.Idle.RetrieveFrom(Npc);
                if (!idleEventRef.IsNull) {
                    IdleAudioEmitter = AlivePrefab.AddComponent<ARFmodEventEmitter>();
                    // IdleAudioEmitter.ChangeEvent(idleEventRef, false);
                    // IdleAudioEmitter.PlayEvent = EmitterGameEvent.ObjectEnable;
                    // IdleAudioEmitter.StopEvent = EmitterGameEvent.ObjectDisable;
                    // if (AlivePrefab.activeInHierarchy) {
                    //     IdleAudioEmitter.Play();
                    // }
                }
            } else {
                Log.Important?.Error($"There is no AlivePrefab inside this NPC!!! This will lead to many errors!!! Prefab: {gameObject.name}", gameObject);
            }

            RootMotion.OnAnimatorMoved += OnAnimatorMoved;
            
            NpcIsGroundedHandler.AddIsGroundedProvider(Npc, this);
            _initialized = true;
            DisableFallDamageFor(10);
        }
        
        void InitRichAIData(RichAI richAI) {
            RichAI.updateRotation = false;
            richAI.groundMask = RenderLayers.Mask.CharacterGround;
            richAI.maxSpeed = CurrentMaxSpeed * Npc.CharacterStats.MovementSpeedMultiplier;
            richAI.rotationSpeed = AngularSpeed;
            richAI.endReachedDistance = 0.25f;
            richAI.radius /= StateCombat.AICombatRadiusScale;
            RichAI.rotation = LogicalRotation;
            richAI.FindComponents();
            richAI.InitPosition(transform.position);
            richAI.SetCustomTimeProvider(Npc.TryGetElement<ARTimeProvider>() ?? Npc.AddElement<ARTimeProvider>());
            richAI.SetCustomVelocitClamper(Npc.TryGetElement<ARDeltaPositionLimiter>() ?? Npc.AddElement<ARDeltaPositionLimiter>());
        }

        void OnMovingPlatformAdded(MovingPlatform movingPlatform) {
            _movingPlatform = movingPlatform;
            _movingPlatform.WithFixedUpdate(ProcessOnElevatorFixedUpdate);
        }
        
        void OnMovingPlatformDiscarded(MovingPlatform movingPlatform) {
            _movingPlatform?.WithoutFixedUpdate(ProcessOnElevatorFixedUpdate);
            _movingPlatform = null;
        }

        void OnDistanceBandChanged(int currentBand) {
            if (LocationCullingGroup.InOverlapRecoveryBand(currentBand)) {
                HeroOverlapRecoveryHandler.AddOverlapRecoveryProvider(this);
            } else {
                HeroOverlapRecoveryHandler.RemoveOverlapRecoveryProvider(this);
            }
        }

        void OnDisable() {
            if (RootMotion != null) {
                RootMotion.enabled = false;
            }

            if (RichAI != null) {
                ToggleGlobalRichAIActivity(false);
            }
            
            HeroOverlapRecoveryHandler.RemoveOverlapRecoveryProvider(this);
            
            // when we disable npc with active teleport request, the teleport will never occur unless we call it here
            TryTeleport();
        }

        void OnEnable() {
            if (RootMotion != null) {
                RootMotion.enabled = true;
            }
            if (RichAI != null) {
                ToggleGlobalRichAIActivity(true);
            }
        }
        
        public void RemoveMovementRefs() {
            RichAI = null;
            RvoController = null;
        }

        protected override void OnDestroy() {
            if (IdleAudioEmitter != null) {
                Destroy(IdleAudioEmitter);
                IdleAudioEmitter = null;
            }
            
            if (_initialized && _movingPlatform?.HasBeenDiscarded == false) {
                _movingPlatform.WithoutFixedUpdate(ProcessOnElevatorFixedUpdate);
            }
            
            _movingPlatform = null;
            base.OnDestroy();
            if (RichAI != null) {
                Destroy(RichAI);
            }
            if (RvoController != null) {
                Destroy(RvoController);
            }
            
            if (Npc is { HasBeenDiscarded: false }) {
                NpcIsGroundedHandler.RemoveIsGroundedProvider(Npc, this);
            }
            HeroOverlapRecoveryHandler.RemoveOverlapRecoveryProvider(this);
        }
        
        public void ToggleGlobalRichAIActivity(bool state) {
            if (_globalRichAIEnabled == state && RichAI.enabled == RichAIEnabled) {
                return;
            }
            _globalRichAIEnabled = state;
            RefreshRichAIActivity();
        }

        public void ToggleIdleOnlyRichAIActivity(bool state) {
            if (_idleRichAIEnabled == state && RichAI.enabled == RichAIEnabled) {
                return;
            }
            _idleRichAIEnabled = state;
            RefreshRichAIActivity();
        }

        public void RefreshRichAIActivity() {
            RichAI.enabled = RichAIEnabled;
        }
        
        public void Move(Vector3 direction, bool useCollisionAvoidance = true, bool overrideDestination = true) {
            if (!RichAI.enabled || Npc == null || Npc.HasBeenDiscarded) {
                return;
            }

            if (Npc.IsStunned || !RichAI.canMove) {
                return;
            }

            direction *= Npc.CharacterStats.MovementSpeedMultiplier;

            ICharacter currentTarget = Npc.GetCurrentTarget();
            float minDistanceToTarget = MinDistanceToTarget;
            if (minDistanceToTarget > 0) {
                direction = AIUtils.LimitDeltaPositionTowardsTarget(currentTarget, RichAI.transform.position, direction, minDistanceToTarget);
            }

            Vector3 desiredPosition = transform.position + direction;

            if (RvoController != null && useCollisionAvoidance) {
                CollisionAvoidanceMoveStep(direction);
            } else {
                RichAI.Move(direction);
            }

            if (overrideDestination && NpcCanMoveHandler.CanOverrideDestination(Npc)) {
                NpcHistorian.NotifyMovement(Npc, $"Set Destination to: {desiredPosition}");
                RichAI.destination = desiredPosition;
            }
        }

        void CollisionAvoidanceMoveStep(Vector3 direction) {
            // RVOController works on its internal loop, whereas NocController's `Move` can be called at any time.
            // Since it sets velocity (and, by extension, a target point) in a non-additive way, we have to accumulate
            // the movement we want to apply between each RVOController's step (which we detect here by checking if
            // its position has changed), otherwise one `Move` call can override another.
            // In the future, we might want to look into manipulating RVOController's state more properly.
            
            if (RvoController.position != _rvoPositionOnLastMove) {
                _rvoPositionOnLastMove = RvoController.position;
                _accumulatedVelocitySinceLastRvoUpdate = Vector3.zero;
            }
            _accumulatedVelocitySinceLastRvoUpdate += direction / this.GetDeltaTime();
            RvoController.Move(_accumulatedVelocitySinceLastRvoUpdate);
        }
        
        public void SetTargetRootRotationFromState(NpcStateType state, float targetRotation) {
            if (!TargetRootRotationInProgress) {
                ResetRootRotationOffset();
            }
            _targetRootRotationActiveStates.Add(state);
            UpdateRootRotationOffsetBy(targetRotation);
        }
        
        public void ResetTargetRootRotation() {
            _targetRootRotationActiveStates.Clear();
            ResetRootRotationOffset();
        }
        
        public void UnmarkTargetRootRotationForState(NpcStateType state) {
            _targetRootRotationActiveStates.Remove(state);
            if (trackingVisualFallbackForce == 0f && !TargetRootRotationInProgress) {
                ResetRootRotationOffset();
            }
        }

        void UpdateRootRotationOffsetBy(float additionalOffset) {
            if (additionalOffset == 0f) {
                return;
            }

            _rootMotionTrackingOffset += additionalOffset;

            RichAI.rotation *= Quaternion.Euler(0, additionalOffset, 0);
            
            float newTarget = _rotation.Target + additionalOffset;
            float newValue = _rotation.Value + additionalOffset;
            _rotation.SetInstant(newTarget);
            _rotation.SetValue(newValue);
        }

        void ResetRootRotationOffset() {
            if (_rootMotionTrackingOffset != 0) {
                UpdateRootRotationOffsetBy(_rootMotionTrackingOffset * -1f);
            }
        }
        
        void OnAnimatorMoved(Animator animator) {
            if (Npc == null || Npc.HasBeenDiscarded) {
                return;
            }

            if (animator.deltaPosition.magnitude > 0.01f) {
                Move(animator.deltaPosition);
            }
            
            if (animator.deltaRotation != Quaternion.identity) {
                float deltaAngle = Mathf.DeltaAngle(0, animator.deltaRotation.eulerAngles.y);

                if (TargetRootRotationInProgress) {
                    _rootMotionTrackingOffset -= deltaAngle;
                } else {
                    _rotation.SetValue(_rotation.Value + deltaAngle);
                    RichAI.rotation *= Quaternion.Euler(0, deltaAngle, 0);
                }
            }
        }
        
        void OnControllerColliderHit(ControllerColliderHit hit) {
            _hitNormal = hit.normal;
        }

        void Update() {
            if (!_initialized || Npc == null || Npc.HasBeenDiscarded || Npc.ParentModel.Interactability != LocationInteractability.Active) {
                return;
            }

            float deltaTime = Npc.GetDeltaTime();
            CheckGrounded();
            if (Npc == null || Npc.HasBeenDiscarded) { // in case npc died from fall damage
                return;
            }
            UpdateRotation(deltaTime);
            bool isInIdle = !Npc.IsSummon && (Npc.NpcAI?.InIdle ?? false);
            bool inCombat = Npc.IsInCombat();
            float maxSpeed = CurrentMaxSpeed * Npc.CharacterStats.MovementSpeedMultiplier;
            RichAI.canMove = NpcCanMoveHandler.CanMove(Npc) && (maxSpeed > 0 || !isInIdle);
            RichAI.canUseGravity = Movement is not { CurrentState: SnapToPositionAndRotate };
            RichAI.slowdownTime = Npc.UseRichAISlowdownTime ? (inCombat ? CombatSlowDownTime : DefaultSlowDownTime) : 0;

            if (NpcCanMoveHandler.ShouldResetMovementSpeed(Npc)) {
                RichAI.maxSpeed = 0f;
            } else if (deltaTime != 0 && RichAI.velocity == Vector3.zero) {
                RichAI.maxSpeed = maxSpeed;
            } else {
                float maxSpeedDelta = deltaTime * (maxSpeed > RichAI.maxSpeed ? accelerationSpeed : decelerationSpeed);
                RichAI.maxSpeed = Mathf.MoveTowards(RichAI.maxSpeed, maxSpeed, maxSpeedDelta);
            }
            
            if (Npc.ParentModel.Interactability != LocationInteractability.Active) {
                return;
            }
            
            RootMotion.OnUpdate(deltaTime);
            MinYCheck();

            if (_hasInCombatParameter) {
                bool animatorInCombat = Animator.GetFloat(InCombat) == 1;
                if (inCombat != animatorInCombat) {
                    ToggleCombatParam(inCombat).Forget();
                }
            }

            HandleRunningIntoWall(deltaTime);
            HandleReachingDestination();
        }

        void HandleRunningIntoWall(float deltaTime) {
            bool shouldRunThroughRvoObstacles = false;
            if (RichAI.enabled && RichAI.canMove && RichAI.maxSpeed > 0f && !RichAI.reachedEndOfPath) {
                if (!_runningIntoWall && _runningIntoWallProlong <= 0) {
                    _runningIntoWallDuration = 0;
                }
                
                _runningIntoWall = CurrentVelocity.magnitude <= 0.25f;
                if (_runningIntoWall) {
                    _runningIntoWallDuration += deltaTime;
                    _runningIntoWallProlong = RunningIntoWallProlong;
                } else {
                    _runningIntoWallProlong -= deltaTime;
                }

                shouldRunThroughRvoObstacles = (_runningIntoWall || _runningIntoWallProlong > 0)
                                    && _runningIntoWallDuration > MinRunningIntoWallDuration;
            } else {
                _runningIntoWallDuration = 0;
                _runningIntoWallProlong = 0;
            }

            RvoController.collidesWith = shouldRunThroughRvoObstacles ? 0 : _originalRvoCollisionLayer;
            if (_runningIntoWallDuration > MaxRunningIntoWallDuration) {
                ReachDestination();
            }
        }
        
        void HandleReachingDestination() {
            if (!_destinationReached && RichAI.reachedEndOfPath && !RichAI.pathPending) {
                ReachDestination();
            }
        }

        void ReachDestination() {
            _runningIntoWallDuration = 0;
            _runningIntoWallProlong = 0;
            _destinationReached = true;
            onReached?.Invoke();
        }

        async UniTaskVoid ToggleCombatParam(bool inCombat) {
            if (await AsyncUtil.DelayTime(Animator, 0.25f)) {
                Animator.SetFloat(InCombat, inCombat ? 1 : 0);
            }
        }
        
        void UpdateRotation(float deltaTime) {
            if (Npc.IsStunned) {
                RichAI.enableRotation = false;
                return;
            }

            PerformRootMotionTrackingOffsetFallback(deltaTime);
            
            RichAI.enableRotation = UseRichAIRotation;
            RichAI.rotationSpeed = AngularSpeed;
            RotationScheme?.Update(deltaTime);
            
            if (RichAI.enableRotation) {
                _rotation.SetInstant(RichAI.rotation.eulerAngles.y);
            } else {
                _rotation.Update(deltaTime, AngularSpeed);
                RichAI.rotation = LogicalRotation;
            }

            Target.SafelyRotateTo(VisualRotation);
        }

        void PerformRootMotionTrackingOffsetFallback(float deltaTime) {
            if (TargetRootRotationInProgress) {
                _timeSinceNoRootRotation = 0f;
                return;
            }

            if (_rootMotionTrackingOffset == 0f || trackingVisualFallbackForce == 0f) {
                return;
            }
            
            _timeSinceNoRootRotation += deltaTime;
            float dampingFactor = trackingVisualFallbackForce * _timeSinceNoRootRotation * AngularSpeedMultiplier;
            _rootMotionTrackingOffset /= 1f + dampingFactor * deltaTime;
            if (math.abs(_rootMotionTrackingOffset) < 0.01f) {
                _rootMotionTrackingOffset = 0.0f;
            }
        }
        
        void CheckGrounded() {
            // set sphere position, with offset
            Grounded = NpcIsGroundedHandler.IsGrounded(Npc);
            if (!Grounded) {
                _hitNormal = Vector3.zero;
                _slopeDir = Vector3.zero; 
                return;
            }
	        
            // --- slope detection
            if (_hitNormal != Vector3.zero && Vector3.Angle(Vector3.up, _hitNormal) >= data.slopeCriticalAngle) {
                Grounded = false;
                _slopeDir += new Vector3 {
                    x = (1f - _hitNormal.y) * _hitNormal.x * data.slopeFriction,
                    z = (1f - _hitNormal.y) * _hitNormal.z * data.slopeFriction
                };
            } else {
                _slopeDir = Vector3.zero;
            }
        }

        void OnFell() {
            if (Time.frameCount <= _fallDamageDisableEndFrame || Time.fixedTime <= _fallDamageDisableEndFixedUpdateTime) {
                return;
            }
            float heightDifference = _yLiftOff - YPosition - DamageNullify;
            heightDifference = Mathf.Max(heightDifference, 0);
            float damage = FallDamageUtil.GetFallDamage(heightDifference);
            if (damage > 0) {
                Vector3 coords = PhysicsBasedPosition;
                NNInfo nearest = AstarPath.active.GetNearest(coords, NNConstraint.Walkable);
                coords = nearest.node != null ? nearest.position : coords;
                Target.SafelyMoveTo(coords);
                Npc.DealFallDamage(damage);
            }
        }

        // === Teleporting
        
        /// <summary>
        /// Requests npc to teleport. It teleports in first FixedUpdate after request. <br/>
        /// Cannot requests many teleports during one update unless context is same. <br/>
        /// If context is the same former request will be overriden.
        /// </summary>
        public void TeleportTo(TeleportDestination destination, TeleportContext context = TeleportContext.None) {
            if (_teleport && (_teleportContext == TeleportContext.None || _teleportContext != context)) {
                Log.Minor?.Error(
                    $"InvalidOperation: Cannot teleport ({context}). Another teleport ({_teleportContext}) is in progress"
                );
                return;
            }
            _teleportDestination = destination;
            _teleportContext = context;
            _teleport = true;
            DisableFallDamageForTeleport();
            
            if (!gameObject.activeInHierarchy) {
                // Try Teleport is performed on Npc Disabling, but when Npc is disabled already it will not happen till it's activated.
                TryTeleportNextFrame().Forget();
            }
        }
        
        public void DisableFallDamageFor(int frames) {
            _fallDamageDisableEndFrame = Mathf.Max(_fallDamageDisableEndFrame, Time.frameCount + frames);
            _fallDamageDisableEndFixedUpdateTime = Mathf.Max(_fallDamageDisableEndFixedUpdateTime, Time.fixedTime + frames * GlobalTime.FixedTimeStep);
        }
        public void DisableFallDamageForTeleport() => DisableFallDamageFor(5);
        public void DisableFallDamageForExitingRagdoll() => DisableFallDamageFor(5);

        void FixedUpdate() {
            TryTeleport();
        }
        
        void ProcessOnElevatorFixedUpdate(float deltaTime, ElevatorPlatform elevatorPlatform) {
            Vector3 position = Position + elevatorPlatform.PositionChange;
            RichAI.FinalizeMovement(position, LogicalRotation, false);
        }
        
        async UniTaskVoid TryTeleportNextFrame() {
            if (!await AsyncUtil.DelayFrame(Npc)) {
                return;
            }
            TryTeleport();
        }

        void TryTeleport() {
            if (!_teleport) {
                return;
            }
            
            if (HasBeenDiscarded) {
                return;
            }
            
            var newPosition = _teleportDestination.position;
		        
            // Teleport
            Target.Trigger(GroundedEvents.BeforeTeleported, Target);
            RichAI.ForceTeleport(newPosition);
            if (!gameObject.activeInHierarchy) {
                Npc.ParentModel.View<VDynamicLocation>()?.SyncPositionAndRotation();
            }
            if (_teleportDestination.Rotation != null) {
                SetRotationInstant(_teleportDestination.Rotation.Value);
            }
            
            Target.Trigger(GroundedEvents.AfterTeleported, Target);

            // Reset state
            _teleport = false;
            _teleportContext = TeleportContext.None;
            _teleportDestination = TeleportDestination.Zero;
        }

        public void SetRotationInstant(Quaternion quaternion) => SetRotationInstant(quaternion.eulerAngles.y);
        public void SetRotationInstant(float y) => _rotation.SetInstant(y);

        void MinYCheck() {
            Vector3 position = Target.Coords;
            if (position.y < -250) {
                Vector3 newPosition = Ground.SnapToGround(position);
                if (position == newPosition) {
                    return;
                }
                TeleportDestination destination = new() {
                    position = newPosition,
                    Rotation = LogicalRotation
                };
                TeleportTo(destination, TeleportContext.MinYCheck);
            }
        }
        
        public void MoveToAbyss() {
            if (_movingToAbyss) {
                return;
            }
            Npc.ParentModel.SetInteractability(LocationInteractability.Hidden);
            if (!NpcPresence.InAbyss(Npc.Coords)) {
                _movingToAbyss = true;
                MoveToAbyssAfterOneFrame().Forget();
            }
        }
        
        async UniTaskVoid MoveToAbyssAfterOneFrame() {
            NpcPresence oldPresence = Npc.NpcPresence;
            if (await AsyncUtil.DelayFrame(Npc) 
                && (Npc.NpcPresence == null || Npc.NpcPresence == oldPresence) 
                && _movingToAbyss) {
                
                Npc.ParentModel.SafelyMoveTo(NpcPresence.AbyssPosition, true);
            }
            _movingToAbyss = false;
        }

        public void AbortMoveToAbyss() {
            _movingToAbyss = false;
        }
        
        // === Controlling
        public void ForceForwardMovement(bool enable) {
            _forcedForwardMovementOnly = enable;
        }
        
        public bool TrySetDestination(Vector3 destination) {
            if (RichAI is not {enabled: true}) {
                return false;
            }
            
            NpcHistorian.NotifyMovement(Npc, $"Set Destination to: {destination}");
            RichAI.destination = destination;
            _runningIntoWall = false;
            _destinationReached = false;
            if (AstarPath.active != null) {
                RichAI.SearchPath();
            }
            return true;
        }

        public void SetRotationScheme(IRotationScheme scheme, VelocityScheme newVelocityScheme) {
            if (scheme == RotationScheme) {
                return;
            }
            
            NpcHistorian.NotifyRotation(Npc, $"Set Rotation Scheme to: {scheme.GetType().Name}");
            
            if (RotationScheme != null) {
                if (RotationScheme.Controller != this) {
                    Log.Important?.Error("Trying to change RotationScheme Controller for scheme that is not owned by this controller!");
                }
                RotationScheme.Controller = null;
            }
            RotationScheme = scheme;
            if (RotationScheme != null) {
                if (RotationScheme.Controller != null) {
                    Log.Important?.Error("Trying to change RotationScheme Controller for scheme that is not owned by this controller!");
                }
                RotationScheme.Controller = this;
                RotationScheme.Enter();
            }

#if UNITY_EDITOR
            if (ForwardMovementOnly && newVelocityScheme != VelocityScheme.NoMove && RotationScheme is not RotateTowardsMovement && RotationScheme is not NoRotationChange) {
                Log.Important?.Warning($"Rotation Scheme changed to: {RotationScheme} even though ForwardMovementOnly is true, are you sure this is desired?");
            }
#endif
        }

        public Vector2 CurrentVelocity => !RichAI.canMove ? Vector2.zero : RichAI.velocity.ToHorizontal2();

        public void FinalizeMovement(bool clampToNavMesh = false) {
            if (RichAI is { isActiveAndEnabled: true } && Npc is { HasBeenDiscarded: false }) {
                RichAI.FinalizeMovement(transform.position, LogicalRotation, clampToNavMesh);
            }
        }

        public float AngularSpeedMultiplier {
            get {
                float multiplier = Npc?.AngularSpeedMultiplier?.Multiplier ?? 1;
                float animancerMultiplier = ARNpcAnimancer != null ? ARNpcAnimancer.AngularSpeedMultiplier : 1;
                return multiplier * animancerMultiplier;
            }
        }
        public float AngularSpeed {
            get {
                float speed = Npc?.NpcAI is { InCombat: true } ? combatAngularSpeed : angularSpeed;
                return speed * AngularSpeedMultiplier;
            }
        }
        public float Rotation {
            get => _rotation.Value;
            set => _rotation.Set(value);
        }

        public float EstimatedAngularVelocity {
            get {
                var steeringAngleDelta = -1.0f * Vector2.SignedAngle(LogicalForward, SteeringDirection);
                return Mathf.Clamp(steeringAngleDelta, -AngularSpeed, AngularSpeed);
            }
        }
        
        public Vector2 SteeringDirection {
            get {
                return UseRichAIRotation switch {
                    true => (RichAI.steeringTarget - Position).ToHorizontal2().normalized,
                    false => Vector2Util.AngleToHorizontal2(_rotation.Target)
                };
            }
            set => Rotation = value.Horizontal2ToAngle();
        }

        public bool UseRichAIRotation => RotationScheme is { UseRichAIRotation: true } && !TargetRootRotationInProgress;
        public Vector2 LogicalForward => Vector2Util.AngleToHorizontal2(_rotation.Value);
        public Vector2 LogicalRight => Vector2Util.AngleToHorizontal2(_rotation.Value + 90.0f);
        Quaternion LogicalRotation => Quaternion.Euler(0, Rotation, 0);
        Quaternion VisualRotation => Quaternion.Euler(0, Rotation - _rootMotionTrackingOffset, 0);


        // === Helping Properties
        public Vector3 Position => transform.position;
        public void SetForwardInstant(Vector2 value) => _rotation.SetInstant(value.Horizontal2ToAngle());

        bool IsGroundedInternal() {
            Vector3 position = PhysicsBasedPosition;
            Vector3 spherePosition = new(position.x, position.y - data.groundedOffset, position.z);
            return Physics.CheckSphere(spherePosition, data.groundedRadius, RenderLayers.Mask.CharacterGround, QueryTriggerInteraction.Ignore);
        }
#if UNITY_EDITOR
        [Button]
        public void DiscardAllOtherNpc() {
            foreach (NpcElement npc in World.All<NpcElement>().ToArraySlow()) {
                if (npc != Movement.ParentModel) {
                    npc.ParentModel.Discard();
                }
            }
        }

        [Button]
        public void ChangeIntoGhost() {
            Npc.AddElement<NpcGhostElement>();
        }
        
        // === Gizmos
        void OnDrawGizmosSelected() {
            DrawGroundedSphere();
            DrawLogicalForwardVector();
        }

        void DrawGroundedSphere() {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = IsGroundedInternal() ? transparentGreen : transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Vector3 position = PhysicsBasedPosition;
            Vector3 spherePosition = new(position.x, position.y - data.groundedOffset, position.z);
            Gizmos.DrawSphere(new Vector3(spherePosition.x, spherePosition.y - data.groundedOffset, spherePosition.z), data.groundedRadius);
        }

        void DrawLogicalForwardVector() {
            Color arrowColor = new Color(1.0f, 0.5f, 0.0f);
            Gizmos.color = arrowColor;
            
            Vector3 position = PhysicsBasedPosition + Vector3.up;
            Vector3 rotationForward = position + LogicalForward.ToHorizontal3() * 5.0f;
            Gizmos.DrawLine(position, rotationForward);
        }
#endif
    }
}