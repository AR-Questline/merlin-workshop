using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    [NoPrefab]
    [RequireComponent(typeof(ARFmodEventEmitter))]
    public class VMount : View<MountElement>, IUIPlayerInput {
        const float PreJumpAnimationLengthSeconds = 0.25f;
        const float UnitTurningDegreesPerSecond = 57.3f;
        const float TimeToCorrectCameraRotation = 3f;

        static PlayerInput Input => World.Any<PlayerInput>();
        public int InputPriority => 1;
        public Transform Saddle => _saddle;
        public DismountPoint[] dismountLocations = Array.Empty<DismountPoint>();
        public MountData.Data Data => Target.MountData.GameplayData;
        public MountData.InputData InputData => Target.MountData.GetInputData(RewiredHelper.IsGamepad);
        Hero MountedHero => Target.MountedHero;
        GameControls GameControls => _gameControls ??= World.Only<SettingsMaster>().ControlsSettings.First();

        GameControls _gameControls;
        CharacterController _controller;
        BoxCollider _interactionCollider;
        ARFmodEventEmitter _emitter;

        GameObject _walkThroughCollider;
        GameObject _dismountLocationParent;
        Transform _saddle;
        Transform _spine;
        Transform _aheadWallDetectionPoint;
        Transform _transform;
        VHeroController _mountedHeroController;

        VMountAnimator _animator;
        MountHeroSeeker _heroSeeker;
        VCMountHorseArmor _armor;

        bool _initialized;
        bool _mounted;
        bool _isSprintToggled;
        bool _isWalkToggled;

        float _remainingTimeToFollowHorseRotation;
        bool _grounded;
        bool _jumped;
        bool _jumpInProgress;
        float _timeRemainingToJump;
        Vector3 _hitNormal;
        Vector3 _groundNormal;

        float _runningVelocity;
        float _turningVelocity;
        float _verticalVelocity;

        bool _inWater;
        float _currentWaterDepth;

        bool _aheadWallHit;
        float _aheadWallDistance;
        
        Vector3 _previousHitNormal;
        Vector3 _previousGroundNormal;
        bool _previousGrounded;
        Vector3 _previousPosition;

        Queue<ControllerColliderHit> _accumulatedHits = new();
        Queue<int> _accumulatedHitsCountPerFrame = new();
        int _accumulatedHitsCountThisFrame;

        Vector3 _initialSaddleToSpineOffset;
        float _neighState;
        
        float _currentFovMultiplier = 1.0f;
        
        Action _afterInitialized;
        ScreenShakesProactiveSetting _screenShakesSetting;

        public bool Grounded => _grounded;
        public bool InWater => _inWater;
        public float RunningVelocity => _runningVelocity;
        public float TurningVelocity => _turningVelocity;
        
        public bool BreakingAheadOfWall => _aheadWallHit && _runningVelocity >= Data.runningSpeed;
        public Vector2 AnimatorParams =>
            new(RunningVelocity * VMountAnimator.ForwardMovementAnimatorScalar,
                TurningVelocity * VMountAnimator.TurningMovementAnimatorScalar);
        public bool CanUseArmor => _armor != null;

        public static class Events {
            public static readonly Event<MountElement, bool> WaterStateChanged = new(nameof(WaterStateChanged));
        }
        
        protected override void OnInitialize() {
            _transform = transform;
            _dismountLocationParent = _transform.GrabChild<Transform>("Saddle/DismountLocations").gameObject;

            dismountLocations = _dismountLocationParent.GetComponentsInChildren<DismountPoint>();
            _dismountLocationParent.SetActive(false);

            _interactionCollider = _transform.GrabChild<BoxCollider>("Interaction Collider");
            _aheadWallDetectionPoint = _transform.GrabChild<Transform>("AheadWallDetectionPoint");
            
            _saddle = _transform.GrabChild<Transform>("Saddle/VMountParent");
            _spine = _transform.GrabChild<Transform>("CG/Pelvis/Spine/Spine1");
            _initialSaddleToSpineOffset = _saddle.InverseTransformPoint(_spine.position) * -1.0f;
            
            _controller = GetComponentInChildren<CharacterController>();
            _emitter = GetComponent<ARFmodEventEmitter>();

            Transform walkThroughTransform = gameObject.FindChildRecursively("WalkThroughCollider", true);
            if (walkThroughTransform) {
                _walkThroughCollider = walkThroughTransform.gameObject;
            }

            _armor = gameObject.GetComponentInChildren<VCMountHorseArmor>();

            _initialized = true;
            _afterInitialized?.Invoke();

            Target.ParentModel.ListenTo(MovingPlatform.Events.MovingPlatformAdded, OnMovingPlatformAdded, this);
            
            // Locations have their dependents disabled automatically based on LocationCullingGroup logic band
            Target.ParentModel.GetOrCreateTimeDependent()
                .WithUpdate(ProcessUpdate)
                .WithLateUpdate(ProcessLateUpdate)
                .WithTimeComponentsOf(gameObject);

            _heroSeeker = new MountHeroSeeker(this);
            _animator = new VMountAnimator(this);
        }

        public void ToggleMountState(bool mounted) {
            if (!_initialized) {
                _afterInitialized += () => ToggleMountState(mounted);
                return;
            }

            _interactionCollider.enabled = !mounted;
            _dismountLocationParent.SetActive(mounted);
            _mounted = mounted;
            if (mounted) {
                _heroSeeker.EndSeeking();
                _mountedHeroController = MountedHero.VHeroController;
            } else {
                _mountedHeroController.HeroCamera.ResetSmoothClampingData();
            }

            _isSprintToggled = false;
            _isWalkToggled = false;

            IgnoreCollisionWithMountedHero(mounted);
        }
        
        public void StartSeekingPlayer() {
            _heroSeeker.TeleportCloserAndStartSeeking();
        }

        public void Teleport(Vector3 position) {
            _controller.enabled = false;
            if (Target.ParentModel.TryGetElement(out GameplayUniqueLocation gameplayUniqueLocation)) {
                gameplayUniqueLocation.TeleportIntoCurrentScene(position);
            } else {
                Target.ParentModel.SafelyMoveTo(position);
            }
            Target.ParentModel.ViewParent.position = position;
            _controller.enabled = true;
        }

        public void Neigh() {
            _neighState = 1.0f;
            PlayAudioClip(AliveAudioType.Roar, true);
        }

        public bool IsMostlyStill() {
            const float Epsilon = 0.001f;
            return Mathf.Abs(_runningVelocity) < Epsilon && Mathf.Abs(_turningVelocity) < Epsilon;
        }

        public bool IsInJump() {
            return _jumped || _jumpInProgress;
        }

        bool IsSprinting() {
            bool sprintPressed = GameControls.IsSprintToggle
                ? _isSprintToggled
                : Input.GetButtonHeld(KeyBindings.Gameplay.Sprint);
            return !_inWater && sprintPressed;
        }

        bool IsWalking() {
            bool walkPressed = GameControls.IsWalkToggle
                ? _isWalkToggled
                : Input.GetButtonHeld(KeyBindings.Gameplay.Walk);
            return !_inWater && walkPressed;
        }
        
        void IgnoreCollisionWithMountedHero(bool state) {
            if (MountedHero == null) return;
            Physics.IgnoreCollision(_mountedHeroController.Controller, _controller, state);
        }

        void OnAnimatorMove() {
            // For now, we don't want animator to apply any kind of movement to mount.
            // This function existing stops all root motion from being applied by animator.
            // See: https://docs.unity3d.com/ScriptReference/Animator-applyRootMotion.html
        }

        void ProcessUpdate(float deltaTime) {
            if (!_initialized) {
                return;
            }

            if (_controller.enabled && _controller.gameObject.activeInHierarchy) {
                HandleMovement(deltaTime);
            }

            _animator.Update(deltaTime);
            UpdateSaddlePosition(deltaTime);
            UpdateMountedHero(deltaTime);

            TiltMountToSlope(deltaTime);
            MakeMovementSound();
        }

        void ProcessLateUpdate(float deltaTime) {
            if (!_initialized) {
                return;
            }

            Target.ParentModel.SafelyMoveTo(_transform.position);
            UpdateWalkThroughCollider();
            if (MountedHero == null) {
                return;
            }

            ClampMountedHeroRotation();
        }

        void HandleMovement(float deltaTime) {
            HandleWaterState();
            HandleJumping(deltaTime);
            ApplyGravity(deltaTime);
            HandleWaterMovement(deltaTime);
            DetectAheadWallState();

            var desiredMovement = GetDesiredMovement(deltaTime);

            HandleRunning(deltaTime, desiredMovement.y);
            HandleTurning(deltaTime, desiredMovement.x);

            LimitForwardMovementAgainstAheadWall(deltaTime);
            
            PerformVerifiedMovement(deltaTime);
        }

        void HandleJumping(float deltaTime) {
            if (_grounded) _jumped = false;

            if (_mounted && Input.GetButtonDown(KeyBindings.Gameplay.Jump)) {
                TryPerformJump();
            }

            if (_jumpInProgress) {
                _timeRemainingToJump -= deltaTime;
                if (_timeRemainingToJump <= 0.0f) {
                    Jump();
                    _jumpInProgress = false;
                }
            }
        }

        void TryPerformJump() {
            if (_grounded && !_jumpInProgress && _runningVelocity >= Data.minimumSpeedForJump && !_inWater) {
                PerformJump();
            }
        }

        void PerformJump() {
            _jumpInProgress = true;
            _animator.UpdateState(VMountAnimator.State.Jump);
            _timeRemainingToJump = PreJumpAnimationLengthSeconds;
        }

        void Jump() {
            _grounded = false;
            _jumped = true;
            _groundNormal = Vector3.zero;
            _hitNormal = Vector3.zero;
            _verticalVelocity = Mathf.Sqrt(2 * Data.jumpHeight * -Data.gravity);
            _animator.UpdateState(VMountAnimator.State.Jump);
        }

        void ApplyGravity(float deltaTime) {
            if (_inWater) return;

            if (_grounded) {
                _verticalVelocity = Data.groundingSnapForce;
            } else {
                if (_previousGrounded && !_jumped) {
                    _verticalVelocity = 0.0f;
                }

                float gravityStep = Data.gravity * GetVerticalVelocityMultiplier() * deltaTime;
                _verticalVelocity += gravityStep;
            }
        }

        void HandleWaterMovement(float deltaTime) {
            if (!_inWater) return;

            if (_grounded && _verticalVelocity < 0.0f) {
                _verticalVelocity = 0.0f;
            }

            _verticalVelocity += Data.bouyancyForce * deltaTime;

            float waterDepthNextFrame = _currentWaterDepth - _verticalVelocity * deltaTime;
            if (waterDepthNextFrame <= Data.waterHoverDepth) {
                _verticalVelocity = -(Data.waterHoverDepth - _currentWaterDepth) / deltaTime;
            } else if (_verticalVelocity < 0.0f) {
                float distanceToMaxDivingDepth = Data.maxDivingWaterDepth - _currentWaterDepth;
                float maxDivingDelta = distanceToMaxDivingDepth * 0.5f;
                _verticalVelocity = Mathf.Max(_verticalVelocity, -maxDivingDelta / deltaTime);
            }
        }

        float GetVerticalVelocityMultiplier() {
            if (_verticalVelocity <= Data.terminalVelocity) {
                return 0.0f;
            } 
            if (_verticalVelocity < Data.fasterFallingThreshold) {
                return Data.fasterFallingMultiplier;
            }

            return 1.0f;
        }

        float GetTargetForwardSpeed() {
            if (IsSprinting()) return Data.sprintingSpeed;
            if (IsWalking()) return Data.walkingSpeed;
            return Data.runningSpeed;
        }

        void HandleRunning(float deltaTime, float input) {
            bool shouldDecelerate = input == 0.0f;
            bool acceleratesOppositeWay = input * _runningVelocity < 0.0f;
            bool movingForward = input > 0;

            float acceleration = _inWater ? Data.swimmingAcceleration : Data.runningAcceleration;
            float deceleration = _inWater ? Data.swimmingDeceleration : Data.runningDeceleration;
            float oppositeWayAcceleration = acceleration + deceleration;
            float directionalAcceleration = acceleratesOppositeWay ? oppositeWayAcceleration : acceleration;
            
            float changeSpeed = shouldDecelerate ? deceleration : directionalAcceleration;
            float targetGroundSpeed = movingForward ? GetTargetForwardSpeed() : Data.backingOffSpeed;
            float targetSpeed = _inWater ? Data.swimmingSpeed : targetGroundSpeed;
            float targetVelocity = input * targetSpeed;

            _runningVelocity = Mathf.MoveTowards(_runningVelocity, targetVelocity, changeSpeed * deltaTime);
        }

        void HandleTurning(float deltaTime, float input) {
            bool shouldDecelerate = input == 0.0f;
            bool acceleratesOppositeWay = input * _turningVelocity < 0.0f;

            float movementDelta = _runningVelocity / Data.runningSpeed;
            float targetGroundSpeed = Mathf.Lerp(Data.turningStationarySpeed, Data.turningGallopSpeed, movementDelta);
            float targetSpeed = _inWater ? Data.turningSwimmingSpeed : targetGroundSpeed;
            float targetVelocity = input * targetSpeed;

            float desiredGroundAcceleration =
                Mathf.Lerp(Data.turningStationaryAccel, Data.turningGallopAccel, movementDelta);
            float desiredGroundDeceleration =
                Mathf.Lerp(Data.turningStationaryDecel, Data.turningGallopDecel, movementDelta);
            float acceleration = _inWater ? Data.turningSwimmingAccel : desiredGroundAcceleration;
            float deceleration = _inWater ? Data.turningSwimmingDecel : desiredGroundDeceleration;
            float oppositeWayAcceleration = acceleration + deceleration;
            float directionalAcceleration = acceleratesOppositeWay ? oppositeWayAcceleration : acceleration;
            float changeSpeed = shouldDecelerate ? deceleration : directionalAcceleration;

            _turningVelocity = Mathf.MoveTowards(_turningVelocity, targetVelocity, changeSpeed * deltaTime);

            float rotationDelta = _turningVelocity * UnitTurningDegreesPerSecond * deltaTime;
            ApplyNewRotation(_transform.rotation * Quaternion.Euler(new Vector3(0, rotationDelta, 0)));
        }

        void ApplyNewRotation(Quaternion rotation) {
            Vector3 positionPreRotation = _transform.TransformPoint(_controller.center);
            _transform.rotation = rotation;
            Vector3 positionPostRotation = _transform.TransformPoint(_controller.center);
            _transform.position -= positionPostRotation - positionPreRotation;
        }

        Vector2 GetDesiredMovement(float deltaTime) {
            var desiredMovement = Vector2.zero;
            if (!_grounded && !_inWater) {
                desiredMovement = GetMidairMovement();
            } else if (_mounted) {
                desiredMovement = GetPlayerDesiredMovement();
            } else if (_heroSeeker.IsSeekingHero) {
                desiredMovement = _heroSeeker.GetDesiredMovement(deltaTime);
            }

            if (_aheadWallHit && _aheadWallDistance < 0.0f) {
                desiredMovement.y = -1.0f;
            }

            return GetClampedInputVector(desiredMovement);
        }

        Vector2 GetMidairMovement() {
            if (!_grounded && _jumped) {
                return new Vector2(0.0f, 1.0f);
            }
            return Vector2.zero;
        }

        Vector2 GetPlayerDesiredMovement() {
            Vector2 rawInput = Input.MountMoveInput;
            Vector2 processedInput = Vector2.zero;

            float inputXSign = Mathf.Sign(rawInput.x);
            float inputXForce = Mathf.Abs(rawInput.x);
            processedInput.x = inputXSign * InputData.turningInputMappingCurve.Evaluate(inputXForce);

            processedInput.y = rawInput.y;
            float maxForwardSpeed = GetTargetForwardSpeed();
            float forwardMovementFactor = Mathf.Clamp01(_runningVelocity / maxForwardSpeed);
            float newInputY = rawInput.y + forwardMovementFactor * InputData.runningForwardInputHelperScale;
            processedInput.y *= Mathf.Max(1.0f, newInputY);

            return processedInput;
        }

        Vector2 GetClampedInputVector(Vector2 input) {
            if (InputData.clampInputMagnitude) {
                return input.sqrMagnitude > 1.0f ? input.normalized : input;
            } 
            
            return new Vector2(Mathf.Clamp(input.x, -1.0f, 1.0f), Mathf.Clamp(input.y, -1.0f, 1.0f));
        }

        void DetectAheadWallState() {
            Vector3 start = _aheadWallDetectionPoint.position;
            Vector3 direction = _aheadWallDetectionPoint.forward;
            float maxDistance = Data.aheadWallDetectionDistance;

            _aheadWallHit = Physics.Raycast(start, direction, out RaycastHit hit, maxDistance, Data.groundLayers);
            if (_aheadWallHit) {
                _aheadWallDistance = hit.distance - Data.aheadWallDesiredDistance;
            }
        }

        void LimitForwardMovementAgainstAheadWall(float deltaTime) {
            if (_aheadWallHit && _aheadWallDistance > 0.0f) {
                float newRunningVelocity = _aheadWallDistance * Data.aheadWallDampeningMultiplier / deltaTime;
                _runningVelocity = Mathf.Min(_runningVelocity, newRunningVelocity);
            }
        }
        
        void PerformVerifiedMovement(float deltaTime) {
            PerformMovement(deltaTime);

            if (ShouldRedoMovementIntoStep()) {
                RevertToLastMovementState();
                PerformMovementNoStep(deltaTime);
            }
        }

        void PerformMovement(float deltaTime) {
            StorePreviousMovementState();
            Vector3 velocityStep = CalculateAbsoluteVelocity() * deltaTime;
            _controller.Move(velocityStep);
            ResolveAccumulatedHits(deltaTime);
        }

        void StorePreviousMovementState() {
            _previousGrounded = _grounded;
            _previousHitNormal = _hitNormal;
            _previousGroundNormal = _groundNormal;
            _previousPosition = _transform.position;
        }

        void RevertToLastMovementState() {
            _grounded = _previousGrounded;
            _hitNormal = _previousHitNormal;
            _groundNormal = _previousGroundNormal;
            _transform.position = _previousPosition;
        }

        bool ShouldRedoMovementIntoStep() {
            if (_grounded || _hitNormal == Vector3.zero) {
                return false;
            }

            Vector3 movementProjectedVector = Vector3.ProjectOnPlane(_runningVelocity * transform.forward, Vector3.up);
            Vector2 stepProjectedVector = Vector3.ProjectOnPlane(_hitNormal, Vector3.up);

            if (Vector3.Dot(movementProjectedVector, stepProjectedVector) > 0.0f) return false;

            Vector3 movementIntoStep = Vector3.ProjectOnPlane(movementProjectedVector, _hitNormal);

            bool shouldRedo = movementIntoStep.magnitude < Data.minimumSpeedForStep;
            return shouldRedo;
        }

        void PerformMovementNoStep(float deltaTime) {
            float stepOffset = _controller.stepOffset;
            _controller.stepOffset = 0.0f;
            PerformMovement(deltaTime);
            _controller.stepOffset = stepOffset;
        }

        Vector3 CalculateAbsoluteVelocity() {
            var forwardVelocity = _runningVelocity * transform.forward;
            var verticalVelocity = Vector3.up * _verticalVelocity;

            var velocity = forwardVelocity + verticalVelocity;
            velocity = PerformSlopeSlideOnVelocity(velocity);

            return velocity;
        }

        Vector3 PerformSlopeSlideOnVelocity(Vector3 velocity) {
            if (!_grounded && _hitNormal != Vector3.zero) {
                return Vector3.ProjectOnPlane(velocity, _hitNormal);
            }
            return velocity;
        }

        void OnControllerColliderHit(ControllerColliderHit hit) {
            AttemptSlideWithHit(hit);
            AccumulateHit(hit);
        }
        
        void AttemptSlideWithHit(ControllerColliderHit hit) {
            NpcController npcController = hit.collider.GetComponentInParent<NpcController>();
            if (npcController == null) {
                return;
            }

            NpcElement npc = npcController.Npc;
            if (npc is not { NpcAI: { HasBeenDiscarded: false } }) {
                return;
            }

            Vector3 pushDir = (hit.collider.transform.position - _transform.position).normalized;
            float hitAngle = Vector2.Angle(pushDir.ToHorizontal2(), _transform.forward.ToHorizontal2());
            bool isValidHitAngle = hitAngle < Data.maximumHitAngleForRagdoll;

            if (_runningVelocity > Data.minimumVelocityForRagdoll && isValidHitAngle) {
                RagdollNpcAway(npc, pushDir);
            } else if (Mathf.Abs(_runningVelocity) > Data.minimumVelocityForPoiseBreak) {
                PushNpcAway(npc, pushDir);
            }
        }

        void PushNpcAway(NpcElement npc, Vector3 pushDir) {
            npc.DealPoiseDamage(NpcStateType.PoiseBreakFront, _runningVelocity, false, false);
            
            float pushForce = Mathf.Max(Mathf.Abs(_runningVelocity), Data.poiseBreakMaxPushForce);
            var pushVector = new Force(pushDir * pushForce, 0.5f);
            
            if (npc.Movement.CurrentState is PushedMovement pushedMovement) {
                pushedMovement.Update(pushVector);
            }
            else if (PushedMovement.CanNpcBePushed(npc)) {
                npc.Movement.InterruptState(new PushedMovement(pushVector, VelocityScheme.SlowWalk));
            }
        }

        void RagdollNpcAway(NpcElement npc, Vector3 pushDir) {
            if (!npc.ParentModel.TryGetElement<EnemyBaseClass>(out var enemyBaseClass)) {
                return;
            }

            if (enemyBaseClass.canBeSlidInto && enemyBaseClass.HasElement<RagdollBehaviour>()) {
                Vector3 ragdollVelocity = _transform.forward * _runningVelocity + pushDir;
                var ragdollMovement = new RagdollMovement(ragdollVelocity, Data.ragdollForceMultiplier, 5f);
                enemyBaseClass.EnableRagdoll(ragdollMovement, true);
            }
        }

        void AccumulateHit(ControllerColliderHit hit) {
            _accumulatedHits.Enqueue(hit);
            _accumulatedHitsCountThisFrame++;
        }

        void ResolveAccumulatedHits(float deltaTime) {
            if (_accumulatedHitsCountThisFrame == 0) {
                ClearAccumulatedHitsQueue();
            }

            LimitRunningVelocityAgainstAllHits(deltaTime);
            RecalculateHitNormalAverage();
            UpdateGroundStatesFromHits();

            MoveAccumulatedHitsQueue();
        }

        void LimitRunningVelocityAgainstAllHits(float deltaTime) {
            Vector3 movementVector = _transform.forward * _runningVelocity;

            foreach (var hit in _accumulatedHits) {
                if (!CanStandOnSurface(hit.normal)) {
                    movementVector = Vector3.ProjectOnPlane(movementVector, hit.normal);
                }
            }

            float targetRunningVelocity = Vector3.Dot(_transform.forward, movementVector);
            float stepFactor = Data.wallHitVelocityDampenSpeed * deltaTime;
            _runningVelocity = Mathf.MoveTowards(_runningVelocity, targetRunningVelocity, stepFactor);
        }

        void RecalculateHitNormalAverage() {
            _hitNormal = Vector3.zero;

            if (_accumulatedHits.Count == 0) {
                return;
            }

            foreach (var hit in _accumulatedHits) {
                _hitNormal += hit.normal;
            }

            _hitNormal.Normalize();
        }

        void UpdateGroundStatesFromHits() {
            Vector3 averageGroundNormal = Vector3.zero;
            foreach (var hit in _accumulatedHits) {
                if (CanStandOnSurface(hit.normal)) {
                    averageGroundNormal += hit.normal;
                }
            }

            if (averageGroundNormal != Vector3.zero) {
                SetGround(averageGroundNormal.normalized);
            } else if (_hitNormal != Vector3.zero && CanStandOnSurface(_hitNormal)) {
                SetGround(_hitNormal);
            } else {
                SetGround(Vector3.zero);
            }
        }

        void SetGround(Vector3 groundNormal) {
            _grounded = groundNormal != Vector3.zero;
            _groundNormal = groundNormal;
        }

        bool CanStandOnSurface(Vector3 normal) {
            return Vector3.Angle(Vector3.up, normal) < Data.slopeCriticalAngle;
        }

        void MoveAccumulatedHitsQueue() {
            _accumulatedHitsCountPerFrame.Enqueue(_accumulatedHitsCountThisFrame);
            _accumulatedHitsCountThisFrame = 0;

            if (_accumulatedHitsCountPerFrame.Count() > Data.framesToAccumulateHitsFor) {
                int hitsToDequeue = _accumulatedHitsCountPerFrame.Dequeue();

                for (int i = 0; i < hitsToDequeue; i++) {
                    _accumulatedHits.Dequeue();
                }
            }
        }

        void ClearAccumulatedHitsQueue() {
            _accumulatedHitsCountPerFrame.Clear();
            _accumulatedHits.Clear();
        }

        void TiltMountToSlope(float deltaTime) {
            Vector3 targetForwardVector = Vector3.ProjectOnPlane(_transform.forward, Vector3.up).normalized;
            Vector3 targetUpVector = Vector3.up;
            if (_grounded) {
                targetForwardVector = Vector3.ProjectOnPlane(_transform.forward, _groundNormal).normalized;
                Vector3 sideVector = Vector3.Cross(targetForwardVector, Vector3.up);
                targetUpVector = Vector3.Cross(sideVector, targetForwardVector);
            }

            Quaternion targetRotation = Quaternion.LookRotation(targetForwardVector, targetUpVector);

            float lerpSpeed = Data.slopeStandTiltRotationSpeed * deltaTime;
            ApplyNewRotation(Quaternion.RotateTowards(_transform.rotation, targetRotation, lerpSpeed));
        }

        void MakeMovementSound() {
            if (MountedHero != null && _grounded) {
                float noiseStrength = NoiseStrength.Strong * Data.horseNoiseStrengthMultiplier;
                float noiseRange = _runningVelocity * Data.horseNoiseRangeMultiplier;

                if (noiseRange > 0) {
                    AINoises.MakeNoise(noiseRange, noiseStrength, false, _transform.position, MountedHero);
                }
            }
        }

        void OnMovingPlatformAdded(MovingPlatform movingPlatform) {
            movingPlatform.ListenTo(MovingPlatform.Events.MovingPlatformStateChanged, OnPlatformStateChanged, this);
        }

        void OnPlatformStateChanged(bool isMoving) {
            _controller.enabled = !isMoving;
        }

        void UpdateSaddlePosition(float deltaTime) {
            Transform saddle = Saddle;
            
            _screenShakesSetting ??= World.Only<ScreenShakesProactiveSetting>();
            
            if (_screenShakesSetting.Enabled) {
                saddle.position = 
                    _spine.position
                    + Vector3.up * _initialSaddleToSpineOffset.y
                    + transform.forward * _initialSaddleToSpineOffset.z;
            } else {
                const float MinimumSlopeFactor = -0.4f;
                const float MaximumSlopeFactor = 0.75f;
                const float HorizontalOffsetScale = 1.0f;
                const float VerticalOffsetScale = 0.75f;
                const float VerticalOffsetPadding = -0.1f;
                
                float slopeFactor = Mathf.Clamp(saddle.forward.y, MinimumSlopeFactor, MaximumSlopeFactor) * -1f;
                float horizontalOffset = slopeFactor * HorizontalOffsetScale;
                float verticalOffset = (Mathf.Abs(slopeFactor) * VerticalOffsetScale) - VerticalOffsetPadding;
                saddle.localPosition = new Vector3(0.0f, verticalOffset, horizontalOffset);
            }
            
            if (_neighState > 0.0f) {
                const float NeighSquashFactor = 0.4f;
                const float NeighHorizontalOffset = -0.8f;
                const float NeighVerticalOffset = 0.5f;
                const float NeighLength = 1.2f;
                
                float neighStateMappedToSine = Mathf.Clamp01(0.5f - Mathf.Cos(_neighState * Mathf.PI * 2.0f) * 0.5f);
                float offsetScale = Mathf.Pow(neighStateMappedToSine, NeighSquashFactor);
                saddle.localPosition += new Vector3(0.0f, NeighVerticalOffset, NeighHorizontalOffset) * offsetScale;
                _neighState -= deltaTime / NeighLength;
            }
        }

        void UpdateMountedHero(float deltaTime) {
            if (MountedHero == null) return;

            MountedHero.MoveTo(_transform.position);
            UpdateMountedHeroFOV();

            if (RewiredHelper.IsGamepad) {
                MakeHeroFollowHorseRotation(deltaTime);
            }
        }
        
        void UpdateMountedHeroFOV() {
            float runningSpeed = Mathf.Min(_runningVelocity, GetTargetForwardSpeed());
            
            float newFovMultiplier = 1f;
            if (runningSpeed > Data.runningSpeed) {
                newFovMultiplier = Data.sprintFovMultiplier;
            } else if (runningSpeed > Data.walkingSpeed) {
                newFovMultiplier = Data.runFovMultiplier;
            }

            if (_currentFovMultiplier != newFovMultiplier) {
                float duration = newFovMultiplier > _currentFovMultiplier
                    ? Data.fovIncreaseChangeDuration
                    : Data.fovDecreaseChangeDuration;
                MountedHero?.FoV.UpdateCustomLocomotionFoVMultiplier(newFovMultiplier, duration);
                _currentFovMultiplier = newFovMultiplier;
            }
        }

        void ClampMountedHeroRotation() {
            Target.HeroTransform.rotation =
                Quaternion.Euler(new Vector3(0, Target.HeroTransform.rotation.eulerAngles.y, 0));
            
            _mountedHeroController.HeroCamera.SetSmoothClampingData(new HeroCamera.SmoothClampingData {
                targetDirection = _transform.forward,
                pitchRange = 180.0f,
                yawRange = InputData.maxHeroHorizontalRotation,
                smoothingRange = 5.0f
            });
        }

        void UpdateWalkThroughCollider() {
            // prevents the collider on moving mount from intersecting NPCs and other mounts,
            // which in turn would cause them to be unnaturally pushed away.
            _walkThroughCollider.SetActive(IsMostlyStill());
        }

        void MakeHeroFollowHorseRotation(float deltaTime) {
            if (Input.LookInput != Vector2.zero) {
                _remainingTimeToFollowHorseRotation = TimeToCorrectCameraRotation;
            } else if (_remainingTimeToFollowHorseRotation < 0) {
                _mountedHeroController.HeroCamera.FollowRotation(_transform.rotation.eulerAngles, deltaTime);
            } else {
                _remainingTimeToFollowHorseRotation -= deltaTime;
            }
        }

        void HandleWaterState() {
            _currentWaterDepth = FindCurrentWaterDepthWithRaycast();

            bool previousWaterState = _inWater;
            _inWater = _currentWaterDepth >= Data.minimumWaterDepthToEnterWater;

            if (previousWaterState != _inWater) {
                Target.Trigger(Events.WaterStateChanged, _inWater);
            }
        }

        float FindCurrentWaterDepthWithRaycast() {
            float colliderHalfHeight = Mathf.Max(_controller.height * 0.5f, _controller.radius);
            Vector3 colliderBottom = _transform.TransformPoint(_controller.center) + Vector3.down * colliderHalfHeight;

            float waterDetectionDistance = Data.maxWaterDetectionDistance;

            Vector3 raycastStart = colliderBottom + Vector3.up * waterDetectionDistance;

            bool hasHitWater = Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, waterDetectionDistance,
                RenderLayers.Mask.Water);

            if (hasHitWater) {
                return waterDetectionDistance - hit.distance;
            }
            
            return 0.0f;
        }

        void OnDisable() {
            Target.Dismount();
        }

        protected override IBackgroundTask OnDiscard() {
            Target.Dismount();
            TimeDependent td = Target?.ParentModel?.GetTimeDependent();

            td?.WithoutUpdate(ProcessUpdate)
                .WithoutLateUpdate(ProcessLateUpdate)
                .WithoutTimeComponentsOf(gameObject);
            return base.OnDiscard();
        }

        // === Audio 
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot, params FMODParameter[] eventParams) {
            EventReference eventRef = audioType.RetrieveFrom(Target);
            PlayAudioClip(eventRef, asOneShot, eventParams);
        }

        public void PlayAudioClip(EventReference eventReference, bool asOneShot, params FMODParameter[] eventParams) {
            if (asOneShot) {
                FMODManager.PlayAttachedOneShotWithParameters(eventReference, gameObject, this, eventParams);
            } else {
                //_emitter.PlayNewEventWithPauseTracking(eventReference, eventParams);
            }
        }

        // === IUIPlayerInput
        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get {
                yield return KeyBindings.Gameplay.Sprint;
                yield return KeyBindings.Gameplay.Walk;
                yield return KeyBindings.Gameplay.Dismount;
            }
        }

        public UIResult Handle(UIEvent evt) {
            if (MountedHero == null) {
                return UIResult.Ignore;
            }

            if (evt is UIKeyDownAction kda) {
                if (kda.Name == KeyBindings.Gameplay.Sprint) {
                    _isWalkToggled = false;
                }
                
                if (kda.Name == KeyBindings.Gameplay.Sprint && GameControls.IsSprintToggle) {
                    _isSprintToggled = !_isSprintToggled;
                    return UIResult.Accept;
                }
                
                if (kda.Name == KeyBindings.Gameplay.Walk && GameControls.IsWalkToggle) {
                    _isWalkToggled = !_isWalkToggled;
                    return UIResult.Accept;
                }

                if (kda.Name == KeyBindings.Gameplay.Dismount) {
                    Target.Dismount();
                    return UIResult.Accept;
                }
            }

            return UIResult.Ignore;
        }
    }
}