using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public abstract partial class HumanoidMovementBase : HeroMovementSystem, IUIPlayerInput {
        static readonly int Slide = Animator.StringToHash("Slide");
        static readonly int DashHash = Animator.StringToHash("Dash");
        static readonly int Movement = Animator.StringToHash("Movement");
        static readonly Vector3 AdditionalUpWhenSwimmingInTPP = Vector3.up * 0.15f;
        
        protected const float DefaultVelocity = -6;
        const float FallingSpeedMultiplier = 2.5f;
        const float MoveSoundRangeMultiplier = 0.5f;
        const float TheftMoveSoundRangeMultiplier = 2f;
        const float WaterSurfaceOffset = 0.33f;
        const float BowingCheckBoxHeight = 0.05f;
        const float SlideIntoDamage = 5f;
        const float SlipFromAISpeed = 0.5f;
        const float MaxSlipFromAIVectorMagnitude = 2.5f;
        const float WaterSurfaceAccumulationTime = 0.2f;
        
        [ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus"), PropertyOrder(1000)]
        bool _isSliding,
             _isDashing,
             _inDashAttack,
             _isWalking,
             _wasOnSlope,
             _isSprintToggled,
             _isWalkToggled;

        [ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus")]
        float _dashTime,
              _dashDuration,
              _dashCooldown,
              _slideTime,
              _jumpAllowedTimeoutDelta,
              _jumpRequestedDelta,
              _onSlopeTime;
        
        bool _attackProlonged;
        bool _wasSprintingEnabled;
        bool _isSneaking;
        bool _isDrawingBow;
        Vector3 _dashVector, _slideVector;
        float _slopeCriticalAngleCos;
        float _slideSpeedCache;
        bool _wasSlidingLastFrame;
        bool _wasSwimmingOnSurfaceLastFrame;
        float _lastWaterSurfaceOffset;
        float _waterSurfaceAccumulatedVel;
        float _waterTopSwimAdjustmentVel;
        
        GameControls _gameControls;
        
        public bool IsSprinting { get; protected set; }
        protected float FallingSpeed { private get; set; }
        
        HeroControllerData Data => Hero.Data;
        CharacterGroundedData GroundedData => Hero.GroundedData;
        
        // === State checks
        bool IsSwimming => Controller.isSwimming;
        bool WantsToJump => _jumpRequestedDelta > 0f;
        bool CanStartSprinting => Controller.Grounded && CanSprint;
        bool CanSprint => Stamina.ModifiedValue > 0 && !IsPerformingAction && !Hero.IsBlocking && !Controller.IsCrouching && !IsSwimming && !Controller.Encumbered;

        LimitedStat Stamina => Hero.Stamina;
        Stat MoveSpeed => Hero.HeroStats.MoveSpeed;
        Stat SprintSpeed => Hero.HeroStats.SprintSpeed;

        bool CanSlide => Hero.Development.CanSlide && !IsPerformingAction && Stamina.ModifiedValue >= Data.slideStaminaCost;
        bool CanJump => _jumpAllowedTimeoutDelta <= 0.0f && !IsPerformingActionFull && (!Controller.IsCrouching || Controller.CanStandUp) && !SlidingOnSlope && !IsSwimming;
        bool CanWaterJump => (_jumpAllowedTimeoutDelta <= 0.0f || TouchingSurfaceValidForWaterJump) && !IsPerformingActionFull && Controller.currentWaterData.hasRaycastHit;
        bool CanDash => Hero.Development.CanDash && !_isDashing && _dashCooldown <= 0 && !IsPerformingActionFull && CheckStaminaBeforeUse(DashCost) && !Controller.Encumbered && Controller.Grounded && !IsSwimming && DashBowCondition;
        
        float DashCost => Hero.HeroStats.DashStamina * Hero.HeroStats.DashCostMultiplier;
        bool SlidingOnSlope => !Controller.Grounded && CollidingWithSlope;
        bool CollidingWithSlope => Controller.hitNormal.y > 0 && Controller.hitNormal.y < _slopeCriticalAngleCos;
        bool TouchingSurfaceValidForWaterJump => (Controller.hitNormal != Vector3.zero && Controller.hitNormal.y >= 0);
        bool DashBowCondition => !_isDrawingBow || Hero.Development.CanDashWhileAiming || Hero.Current.LogicModifiers.DisableBowPullMovementPenalties;
        
        GameControls GameControls => _gameControls ??= World.Any<SettingsMaster>()?.ControlsSettings.First();
        bool SettingsSprintIsToggle => GameControls.IsSprintToggle;
        bool SettingsWalkIsToggle => GameControls.IsWalkToggle;
        bool SettingsCrouchIsToggle => GameControls.IsCrouchToggle;

        bool IsPerformingActionFull => IsPerformingAction || Hero.IsPerformingAction;
        bool IsOrJustWasSliding => _isSliding || _wasSlidingLastFrame; 
        
        // === Public state checks

        public bool IsPerformingAction => _isDashing || _isSliding || Controller.isKicking;
        
        // === HeroMovementSystem
        public override bool CanCurrentlyBeOverriden => true;
        public override bool RequirementsFulfilled => true;

        protected override void Init() {
            Hero.ListenTo(ICharacter.Events.OnBowDrawStart, _ => _isDrawingBow = true, this);
            Hero.ListenTo(ICharacter.Events.OnBowDrawEnd, _ => _isDrawingBow = false, this);
            Hero.ListenTo(Hero.Events.DashForward, LungeDash, this);

            Controller.Input.RegisterPlayerInput(this, this);
            // reset our timeouts on start
            _jumpAllowedTimeoutDelta = Data.jumpTimeout;

            _slopeCriticalAngleCos = math.cos(GroundedData.slopeCriticalAngle * math.TORADIANS);
        }

        public override void Update(float deltaTime) {
            HandleJumpStates(deltaTime);
            TryGroundJump();
            Controller.PerformGroundChecks(deltaTime);
            HeadCheck();
            Controller.PerformWaterCheck(deltaTime);
            TryWaterJump();
            EvaluateWaterSurface(deltaTime);
            SlideOnSlope();
            HandleCrouch();
            HandleSliding(deltaTime);
            HandleDashing(deltaTime);
            Move(deltaTime);
            MinYCheck();
            UpdateAnimator();

            _wasSlidingLastFrame = _isSliding;
        }

        public override void FixedUpdate(float deltaTime) {
            HandleGravity(deltaTime);
        }
        
        protected override void SetupForceExitConditions() { 
            // None
        }

        public override void OnControllerColliderHit(ControllerColliderHit hit) {
            base.OnControllerColliderHit(hit);
            if (hit?.collider == null) {
                return;
            }

            HandleControllerColliderHitWithNPC(hit);
            HandleControllerColliderHitWithMount(hit);
        }

        void HandleControllerColliderHitWithNPC(ControllerColliderHit hit) {
            NpcController npcController = hit.collider.GetComponentInParent<NpcController>();
            if (npcController == null) {
                return;
            }

            NpcElement npc = npcController.Npc;
            NpcAI npcAI = npc?.NpcAI;
            if (npcAI is not { HasBeenDiscarded: false }) {
                return;
            }

            SlipAwayFrom(npc.ParentModel);
            
            // Can't push enemies in combat
            if (npcAI is { InIdle: false, InFlee: false }) {
                return;
            }
            
            // Only sprints pushes NPCs
            if (!IsSprinting && !_isSliding) {
                return;
            }
            
            // Weak pushes are not pushing npcs
            if (Controller.HorizontalVelocity.sqrMagnitude < 5f * 5f) {
                return;
            }
            
            // We don't want to push objects below us
            if (hit.moveDirection.y < -0.3) {
                return;
            }
            
            NpcMovement npcMovement = npcController.Movement;
            // Can't leave Ragdoll State
            if (npcMovement.CurrentState is RagdollMovement) {
                return;
            }

            // Calculate push direction from move direction,
            // we only push objects to the sides never up and down
            var pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            
            if (_isSliding) {
                SlidIntoEnemy(npc, pushDir).Forget();
                return;
            }

            // Can't push enemies, they should attack player instead of moving away from him
            if (npc.IsHostileTo(Hero)) {
                npcAI.EnterCombatWith(Hero);
                return;
            }
            
            var pushForce = new Force(pushDir * Data.pushForce, Data.pushDuration);
            if (npcMovement.CurrentState is PushedMovement pushedMovement) {
                pushedMovement.Update(pushForce);
                return;
            }

            if (PushedMovement.CanNpcBePushed(npc)) {
                npc.TryGetElement<BarkElement>()?.BumpedInto();
                npcMovement.InterruptState(new PushedMovement(pushForce, VelocityScheme.SlowWalk));
            }
        }

        void HandleControllerColliderHitWithMount(ControllerColliderHit hit) {
            VMount mount = hit.collider.GetComponentInParent<VMount>();
            if (mount == null) {
                return;
            }
            
            SlipAwayFrom(mount.Target.ParentModel);
        }

        void SlipAwayFrom(Location location) {
            if (Hero.Grounded) return;
            
            Controller.isSlippingFromAI = true;
            Controller.slipFromAIVector = Vector3.ClampMagnitude(
                Controller.slipFromAIVector +
                (Hero.Coords.ToHorizontal3() - location.Coords.ToHorizontal3()).normalized * SlipFromAISpeed,
                MaxSlipFromAIVectorMagnitude);
        }

        // === IUIPlayerInput
        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get {
                yield return KeyBindings.Gameplay.Walk;
                yield return KeyBindings.Gameplay.Sprint;
            }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction kda) {
                if (kda.Name == KeyBindings.Gameplay.Walk && SettingsWalkIsToggle) {
                    _isWalkToggled = !_isWalkToggled;
                    return UIResult.Accept;
                }

                if (kda.Name == KeyBindings.Gameplay.Sprint && SettingsSprintIsToggle) {
                    _isSprintToggled = !_isSprintToggled;
                    return UIResult.Accept;
                }
            }

            return UIResult.Ignore;
        }
        
        // === Jumping
        void HandleJumpStates(float deltaTime) {
            if (_jumpAllowedTimeoutDelta >= 0.0f) {
                _jumpAllowedTimeoutDelta -= deltaTime;
            }
            
            if (!Controller.Grounded || SlidingOnSlope) {
                _jumpAllowedTimeoutDelta = Controller.Data.jumpTimeout;
            }

            if (_jumpRequestedDelta >= 0.0f) {
                _jumpRequestedDelta -= deltaTime;
            }

            if (Controller.Input.GetButtonDown(KeyBindings.Gameplay.Jump)) {
                _jumpRequestedDelta = Controller.Data.jumpBufferTime;
            }
        }

        void TryGroundJump() {
            if (!WantsToJump || !CanJump) {
                return;
            }
            
            if (IsSprinting && Hero.IsInCombat()) {
                if (!CheckStaminaBeforeUse(Controller.Data.jumpStaminaCost)) {
                    return;
                }
                if (!Stamina.DecreaseBy(Controller.Data.jumpStaminaCost)) {
                    return;
                }
            }
            Jump();
        }

        void TryWaterJump() {
            if (!WantsToJump || !CanWaterJump) {
                return;
            }

            float waterSurfaceDistance = math.abs(Controller.currentWaterData.distanceToWaterSurface - Controller.Data.swimmingOffset);
            bool swimmingAtWaterSurface = IsSwimming && waterSurfaceDistance < WaterSurfaceOffset;
            bool justLeftWaterSurface = !IsSwimming && Controller.waterLeaveTimeout > 0f;
            
            if ((swimmingAtWaterSurface || justLeftWaterSurface)) {
                Jump();
            }
        }
        
        void Jump() {
            _jumpAllowedTimeoutDelta = Data.jumpTimeout;
            Controller.Grounded = false;
            Controller.OnHeroJumped();
            
            float jumpMultiplier = IsSprinting ? 1.2f : (IsSwimming ? 1.3f : 1);
            float jumpHeight = Hero.HeroStats.JumpHeight * jumpMultiplier;
            // the square root of H * -2 * G = how much velocity needed to reach desired height
            Controller.verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Data.gravity);

            Hero.Trigger(Hero.Events.HeroJumped, true);
            Hero.TryGetElement<CameraShakesFSM>()?.SetCurrentState(HeroStateType.JumpStart);
        }
        
        protected void HeadCheck(bool checkWhenStanding = false) {
            Controller.GetHeadCheckParams(out var centerPosition, out var halfExtents, out var bowingMinCameraHeight, out var bowingCheckCenter,
                out var bowingCheckHalfExtents, out var bowingCheckDistance);
            if (bowingCheckDistance > 0 && Physics.BoxCast(bowingCheckCenter, bowingCheckHalfExtents, Vector3.up, out var hit, Quaternion.identity,
                    bowingCheckDistance, Controller.Data.headObstacleLayers)) {
                Controller.bowingCameraHeight = bowingMinCameraHeight + hit.distance - BowingCheckBoxHeight - Controller.currentHeightData.bowingCheckAdditionalLength;
            } else {
                // some big number for system to recognize we do not need to bow
                Controller.bowingCameraHeight = 100;
            }

            if (checkWhenStanding || Controller.IsCrouching) {
                Controller.headCollided = Physics.CheckBox(centerPosition, halfExtents, Quaternion.identity, Controller.Data.headObstacleLayers);
            } else {
                Controller.headCollided = false;
            }
        }
        
        
        // === Crouch
        void HandleCrouch() {
            if (IsSwimming) {
                return;
            }

            if (Controller.IsSprinting) {
                if (Controller.Input.GetButtonDown(KeyBindings.Gameplay.Crouch) && CanSlide && Stamina.ModifiedValue > Controller.Data.slideStaminaCost) {
                    Controller.audioAnimator.SetTrigger(Slide);
                } else if (Controller.IsCrouching) {
                    Controller.ToggleCrouch(forceTo: false);
                }
            } else {
                if (!_isSliding) {
                    if (SettingsCrouchIsToggle) {
                        if (Controller.Input.GetButtonDown(KeyBindings.Gameplay.Crouch) || (Controller.IsCrouching && Controller.Input.GetButtonHeld(KeyBindings.Gameplay.Sprint))) {
                            Controller.ToggleCrouch();
                        }
                    } else {
                        if (!Controller.IsCrouching && Controller.Input.GetButtonDown(KeyBindings.Gameplay.Crouch)) {
                            Controller.ToggleCrouch(forceTo: true);
                        } else if (Controller.IsCrouching && (Controller.Input.GetButtonUp(KeyBindings.Gameplay.Crouch) || Controller.Input.GetButtonHeld(KeyBindings.Gameplay.Sprint))) {
                            Controller.ToggleCrouch(forceTo: false);
                        }
                    }
                }
            }
        }
        
        async UniTaskVoid SlidIntoEnemy(NpcElement npc, Vector3 pushDir) {
            if (npc.ParentModel.TryGetElement<EnemyBaseClass>(out var enemyBaseClass) && enemyBaseClass.canBeSlidInto && enemyBaseClass.HasElement<RagdollBehaviour>()) {
                DamageParameters parameters = DamageParameters.Default;
                parameters.Direction = pushDir;
                parameters.ForceDirection = pushDir;
                parameters.ForceDamage = 1;
                Damage damage = new(parameters, Hero, npc, new RawDamageData(SlideIntoDamage));
                npc.HealthElement.TakeDamage(damage);
                if (!await AsyncUtil.DelayFrame(this, 2)) {
                    return;
                }
                npc.TryGetElement<BarkElement>()?.SlidedInto();
                enemyBaseClass.EnableRagdoll(new RagdollMovement(pushDir + (Vector3.up * 0.3f), Data.slidePushForce, 5f), true);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        protected virtual void ToggleCrouch(float duration = 0.5f, bool? forceTo = null) {
            Controller.ToggleCrouch(duration, forceTo);
        }
        
        // === Slide
        void HandleSliding(float deltaTime) {
            const float SlideDuration = 0.7f;
            
            if (_isSliding) {
                _slideTime += deltaTime;

                if (_slideTime > SlideDuration) {
                    EndSliding();
                }
            }
        }
        
        public void SlideBegun() {
            _isSliding = true;
            _slideTime = 0f;
            _slideSpeedCache = SprintSpeed * 1.1f * Hero.CharacterStats.MovementSpeedMultiplier;
            Hero.FoV.UpdateHeroSlidedFoV(true);
            if (Hero.IsInCombat()) {
                Stamina.DecreaseBy(Data.slideStaminaCost);
            }

            _slideVector = Controller.Transform.forward;
            Hero.Trigger(Hero.Events.HeroSlid, true);
            Controller.ToggleCrouch(0.1f, forceTo: true);
        }

        void EndSliding() {
            _slideTime = 0f;
            _isSliding = false;
            Hero.FoV.UpdateHeroSlidedFoV(false);
        }

        // === Dash
        void HandleDashing(float deltaTime) {
            if (_isDashing) {
                _dashTime += deltaTime;
            } else if (_dashCooldown > 0f) {
                _dashCooldown -= deltaTime;
            }

            if (_isDashing && _dashTime >= Data.dashDuration) {
                FinishDashing();
            }
            
            if (Controller.Input.GetButtonDown(KeyBindings.Gameplay.Dash) && CanDash && Stamina.DecreaseBy(DashCost)) {
                Dash(GetDashInputVector());
                RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Short);
            }
        }

        void LungeDash() {
            _inDashAttack = true;
            Dash(Vector2.up);
        }
        
        void Dash(Vector2 dashInputVector) {
            _dashVector = Vector3.Normalize(
                Controller.Transform.right * dashInputVector.x + 
                Controller.Transform.forward * dashInputVector.y);

            Controller.audioAnimator.SetTrigger(DashHash);
            Hero.FoV.UpdateHeroDashedFoV(true);
            Hero.Trigger(CameraShakesFSM.Events.DashHeroCamera, dashInputVector);
            if (!_inDashAttack) {
                CharacterInvulnerability.ApplyInvulnerability(Hero, new TimeDuration(Data.dashGodModeDurationSeconds));
            }
            _dashTime = 0f;
            _isDashing = true;
            
            Hero.Trigger(Hero.Events.HeroDashed, _inDashAttack);
        }

        Vector2 GetDashInputVector() {
            int vertical = 0;
            int horizontal = 0;

            Vector3 moveInput = Controller.Input.MoveInput;
            const float DashInputThreshold = 0.4f;
            if (Mathf.Abs(moveInput.y) > DashInputThreshold) {
                vertical = (int)Mathf.Sign(moveInput.y);
            }
            if (Mathf.Abs(moveInput.x) > DashInputThreshold) {
                horizontal = (int)Mathf.Sign(moveInput.x);
            }
            
            // dashing backwards is default when no input
            if (horizontal == 0 && vertical == 0) {
                vertical = -1;
            }

            return new Vector2(horizontal, vertical);
        }

        void FinishDashing() {
            _isDashing = false;
            _dashTime = 0f;
            _dashCooldown = Data.dashCooldown;
            _inDashAttack = false;
            Hero.FoV.UpdateHeroDashedFoV(false);
            Vector3 horizontalVelocity = Controller.HorizontalVelocity;
            float magnitude = Mathf.Clamp(Controller.HorizontalVelocity.magnitude, 0, MoveSpeed);
            Controller.Controller.SimpleMove(Vector3.ClampMagnitude(horizontalVelocity, magnitude));
            
            Hero.Trigger(Hero.Events.AfterHeroDashed, true);
        }
        
        // === Core Movement
        void Move(float deltaTime) {
            if (deltaTime == 0) {
                return;
            }
            
            Vector2 inputVector = GetInputVector();
            Vector3 moveVector = GetDesiredMovementVector(inputVector);
            
            ApplyAdditionalMovementFromGravityMarker(inputVector != Vector2.zero);
            ResolveSprintingState(deltaTime, inputVector);
            ResolveWalkingState();
            UpdateSneakingState(inputVector);

            float targetSpeed = GetModifiedTargetSpeed(inputVector);

            Vector3 velocity = Controller.HorizontalVelocity;
            if (IsSwimming) {
                ApplyWaterMovement(ref velocity, moveVector, targetSpeed, deltaTime);
            } else if (!Controller.Grounded) {
                ApplyAirMovement(ref velocity, moveVector, targetSpeed, deltaTime);
            } else if (_isDashing || _isSliding) {
                ApplyLinearMovement(ref velocity, moveVector, targetSpeed);
            } else {
                ApplyWalkMovement(ref velocity, moveVector, targetSpeed, deltaTime);
            }

            Controller.isSlippingFromAI = false;
            Vector3 verticalVel = new Vector3(0.0f, Controller.verticalVelocity, 0.0f);
            ApplyMovement(deltaTime, velocity + Controller.additionalMoveVector + Controller.slipFromAIVector, verticalVel);
            Controller.additionalMoveVector = Vector3.zero;
            
            if (!Controller.isSlippingFromAI) {
                Controller.slipFromAIVector = Vector3.zero;
            }
        }
        
        void ApplyAdditionalMovementFromGravityMarker(bool isPlayerMoving) {
            if (Hero.TryGetElement<GravityMarker>(out var gravityMarker)) {
                Controller.additionalMoveVector += gravityMarker.Zone.GetDirectionTowardsCenter(Controller.Transform.position, isPlayerMoving);
            }
        }

        Vector3 GetDesiredMovementVector(Vector2 moveVector) {
            if (_isSliding) {
                return _slideVector.ToHorizontal3();
            }
            if (_isDashing) {
                return _dashVector.ToHorizontal3();
            } 
            if (IsSwimming) {
                Transform headTransform = Hero.TppActive
                    ? Controller.FirePoint.transform
                    : Controller.CinemachineHeadTarget.transform;
                Vector3 forward = headTransform.forward;
                if (Hero.TppActive) {
                    forward = (forward + AdditionalUpWhenSwimmingInTPP).normalized;
                }
                var outMoveVector = headTransform.right * moveVector.x + forward * moveVector.y;
                if (Controller.Input.GetButtonHeld(KeyBindings.Gameplay.Jump)) {
                    outMoveVector.y += 1.0f;
                } 
                if (Controller.Input.GetButtonHeld(KeyBindings.Gameplay.Crouch)) {
                    outMoveVector.y -= 1.0f;
                }
                return Vector3.ClampMagnitude(outMoveVector, 1.0f);
            } 
            if (moveVector == Vector2.zero) {
                return Vector3.zero;
            } 
            
            return Controller.Transform.right * moveVector.x + Controller.Transform.forward * moveVector.y;
        }

        void ApplyWaterMovement(ref Vector3 velocity, Vector3 desiredMovementVector, float targetSpeed, float deltaTime) {
            velocity.y += FallingSpeed;
            
            float waveImpactTopRange = Data.wavesImpactRange;
            float waveImpactBottomRange = waveImpactTopRange + Data.wavesImpactFalloff;
            float waveImpact = Mathf.InverseLerp(waveImpactBottomRange, waveImpactTopRange,
                Controller.currentWaterData.distanceToWaterSurface);
            float waterSurfaceVelocityOffset = _waterSurfaceAccumulatedVel * waveImpact;
            
            velocity.y -= waterSurfaceVelocityOffset;
            velocity.y -= _waterTopSwimAdjustmentVel;
            
            ApplyDrag(ref velocity, Data.waterDragCoefficient, Data.minWaterDrag, deltaTime);
            ApplyAcceleratedMovement(ref velocity, desiredMovementVector, Data.swimAcceleration, targetSpeed, deltaTime);
            
            velocity.y += waterSurfaceVelocityOffset;
            
            if (Controller.currentWaterData.hasRaycastHit) {
                float maxSwimDistance = Controller.currentWaterData.distanceToWaterSurface - Data.swimmingOffset;
                _waterTopSwimAdjustmentVel = Mathf.Min(0.0f, maxSwimDistance / deltaTime - velocity.y);
            }
            
            velocity.y += _waterTopSwimAdjustmentVel;
        }

        void ApplyAirMovement(ref Vector3 velocity, Vector3 desiredMovementVector, float targetSpeed, float deltaTime) {
            ApplyDrag(ref velocity, Data.airDragCoefficient, Data.minAirDrag, deltaTime);
            
            Vector3 movementVector = desiredMovementVector;
            
            if (CollidingWithSlope) {
                var allowedMovementAxis = Vector3.Cross(Vector3.up, Controller.hitNormal);
                movementVector = Vector3.Project(movementVector, allowedMovementAxis);
            }
            
            float midairAcceleration = Controller.verticalVelocity > 0
                ? Controller.Data.midairUpwardsSpeed
                : Controller.Data.midairFallingSpeed;
            
            ApplyAcceleratedMovement(ref velocity, movementVector, midairAcceleration, targetSpeed, deltaTime);
        }

        void ApplyDrag(ref Vector3 velocity, float dragCoefficient, float minDrag, float deltaTime) {
            // drag is proportional to square of velocity
            float dragDelta = math.max(minDrag, dragCoefficient * velocity.sqrMagnitude);
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, dragDelta * deltaTime);
        }

        void ApplyWalkMovement(ref Vector3 velocity, Vector3 desiredMovementVector, float targetSpeed, float deltaTime) {
            ApplyGroundFriction(ref velocity, deltaTime);
            
            float baseAcceleration = IsSprinting ? Controller.Data.sprintAcceleration : 
                                       _isWalking ? Controller.Data.walkAcceleration :
                                       Controller.Data.moveAcceleration;
            
            ApplyAcceleratedMovement(ref velocity, desiredMovementVector, baseAcceleration, targetSpeed, deltaTime);

            Vector3 worldInputNormalizedDirection = desiredMovementVector.normalized;
            bool suddenlyChangedDirection = Vector3.Dot(worldInputNormalizedDirection, velocity) < 0;
            if (suddenlyChangedDirection) {
                velocity = Vector3.ProjectOnPlane(velocity, worldInputNormalizedDirection);
            }
        }

        void ApplyGroundFriction(ref Vector3 velocity, float deltaTime) {
            // surface friction is linearly proportional to velocity
            float groundFriction = Controller.Data.groundFrictionCoefficient * velocity.magnitude;
            groundFriction = math.max(Controller.Data.minGroundFriction, groundFriction);
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, groundFriction * deltaTime);
        }

        void ApplyAcceleratedMovement(ref Vector3 velocity, Vector3 desiredMovementVector, float acceleration, float targetSpeed, float deltaTime) {
            var accelerationVector = desiredMovementVector * (acceleration * deltaTime);
            var movementForce = desiredMovementVector.magnitude;
            float desiredMovementMagnitude = math.max(velocity.magnitude, targetSpeed * movementForce);
            velocity = Vector3.ClampMagnitude(velocity + accelerationVector, desiredMovementMagnitude);
        }

        void ApplyLinearMovement(ref Vector3 velocity, Vector3 desiredMovementVector, float targetSpeed) {
            velocity = desiredMovementVector * targetSpeed;
        }

        void ResolveSprintingState(float deltaTime, Vector2 moveVector) {
            bool canSprintNow = SprintingRequirementsMet(moveVector) || IsOrJustWasSliding;
            if ((Controller.Grounded || IsSwimming) && !canSprintNow) {
                _isSprintToggled = false;
            }
            
            bool wantToSprint = SettingsSprintIsToggle
                ? _isSprintToggled
                : Controller.Input.GetButtonHeld(KeyBindings.Gameplay.Sprint);

            bool shouldSprint = wantToSprint && canSprintNow;
            
            if (shouldSprint && Hero.IsInCombat()) {
                var staminaCost = Controller.Data.sprintCostPerTick * Hero.CharacterStats.SprintCostMultiplier;
                shouldSprint = StaminaUsedUpEffect.TryDecreaseContinuously(staminaCost, deltaTime);
            }
            TrackSprintingState(shouldSprint);
        }

        void ResolveWalkingState() {
            bool wantToWalk;
            if (IsSprinting) {
                wantToWalk = false;
                _isWalkToggled = false;
            } else {
                wantToWalk = (SettingsWalkIsToggle && _isWalkToggled) || Controller.Input.GetButtonHeld(KeyBindings.Gameplay.Walk);
            }
            TrackWalkingState(wantToWalk);
        }

        Vector2 GetInputVector() {
            Vector2 inputVector = Controller.Input.MoveInput;
            if (inputVector.sqrMagnitude >= 1) inputVector.Normalize();

            if (IsSprinting && inputVector.y > 0) {
                inputVector.x = math.clamp(inputVector.x, -0.25f, 0.25f);
                inputVector.y = 1;
            }

            return inputVector;
        }

        float GetModifiedTargetSpeed(Vector2 inputVector) {
            if (_isSliding) {
                return _slideSpeedCache;
            }
            
            return GetTargetSpeed(inputVector) * Hero.CharacterStats.MovementSpeedMultiplier;
        }
        
        float GetTargetSpeed(Vector2 inputVector) {
            if (_isDashing) {
                var dashTargetSpeed = Controller.Data.dashMovementCurve.Evaluate(_dashTime);
                if (!_inDashAttack) {
                    dashTargetSpeed *= Hero.HeroStats.DashSpeed;
                } else {
                    // we use the base value so that lunge attack speed is affected by settings but not by dash speed tweaks
                    dashTargetSpeed *= Hero.HeroStats.DashSpeed.BaseValue;
                }
                return dashTargetSpeed;
            }

            if (Controller.isKicking) {
                return 0f;
            }

            if (!Controller.IsSwimming && inputVector == Vector2.zero) {
                return 0f;
            }

            float targetSpeed = GetBaseTargetSpeed(inputVector);

            if (IsSwimming) {
                targetSpeed *= Hero.HeroStats.SwimSpeed;
            } else {
                if (Controller.IsCrouching) {
                    targetSpeed *= Controller.Data.crouchingSpeedMultiplier * Hero.HeroStats.CrouchSpeedMultiplier.ModifiedValue;
                }
                if (Hero.IsBlocking) {
                    targetSpeed *= Controller.Data.blockingMultiplier;
                }
            }

            return targetSpeed;
        }

        float GetBaseTargetSpeed(Vector3 inputVector) {
            float targetSpeed = MoveSpeed;
            if (inputVector.y < 0.0f) {
                float backwardMultiplier = math.lerp(1.0f, Controller.Data.backwardMultiplier, inputVector.y * -1);
                targetSpeed *= backwardMultiplier;
            }
            if (IsSprinting && inputVector.y > 0.0f) {
                targetSpeed = SprintSpeed;
            }
            if (_isWalking) {
                targetSpeed = math.min(Controller.Data.walkSpeed, targetSpeed);
            }

            return targetSpeed;
        }

        void ApplyMovement(float deltaTime, Vector3 velocity, Vector3 verticalVel) {
            float previousY = Controller.Transform.position.y;
            Controller.PerformMoveStep(velocity * deltaTime + verticalVel * deltaTime);
            FallingSpeed = (Controller.Transform.position.y - previousY) / deltaTime;

            if (FallingSpeed >= 0 && Controller.verticalVelocity < 0) {
                Controller.verticalVelocity = DefaultVelocity;
            }

            Controller.ApplyTransformToTarget();

            MakeMovementSound(velocity, deltaTime);
        }

        void UpdateSneakingState(Vector3 moveTowards) {
            bool wasSneaking = _isSneaking;
            _isSneaking = moveTowards.magnitude > 0 && Controller.IsCrouching;
            
            if (wasSneaking != _isSneaking) {
                Hero.Trigger(Hero.Events.SneakingStateChanged, _isSneaking);
            }
        }

        void EvaluateWaterSurface(float deltaTime) {
            if (deltaTime == 0) {
                return;
            }
            
            bool swimmingOnSurface = IsSwimming && Controller.currentWaterData.hasRaycastHit;

            if (swimmingOnSurface && _wasSwimmingOnSurfaceLastFrame) {
                float currentWaterSurfaceOffset = Controller.currentWaterData.waterSurfaceOffset;
                float waterSurfaceDeltaThisFrame = currentWaterSurfaceOffset - _lastWaterSurfaceOffset;

                // accumulating water surface velocity to prevent jitter caused by height sampling
                _waterSurfaceAccumulatedVel += waterSurfaceDeltaThisFrame / WaterSurfaceAccumulationTime;
                _waterSurfaceAccumulatedVel *= WaterSurfaceAccumulationTime / (WaterSurfaceAccumulationTime + deltaTime);

                _waterSurfaceAccumulatedVel = waterSurfaceDeltaThisFrame;
            } else {
                _waterSurfaceAccumulatedVel = 0.0f;
            }

            _lastWaterSurfaceOffset = Controller.currentWaterData.waterSurfaceOffset;
            _wasSwimmingOnSurfaceLastFrame = swimmingOnSurface;
        }
        
        void HandleGravity(float deltaTime) {
            if (IsSwimming) {
                if (Controller.waterLeaveTimeout == 0 || Controller.verticalVelocity < 0) {
                    Controller.verticalVelocity = 0.0f;
                }

                return;
            }

            var gravityMarker = Hero.GravityMarker;
            if (gravityMarker != null) {
                float newGravity = gravityMarker.Zone.GetGravityForce();
                if (Controller.Grounded && newGravity > 0) {
                    if (Controller.verticalVelocity < 0) {
                        Controller.verticalVelocity = 0;
                    }
                    
                    Controller.Grounded = false;
                }

                Controller.verticalVelocity += newGravity * deltaTime;
                float gravityLimit = gravityMarker.Zone.GetMaxGravityVelocity();

                float gravityLowerLimit = gravityLimit > 0 ? float.NegativeInfinity : gravityLimit;
                float gravityUpperLimit = gravityLimit < 0 ? float.PositiveInfinity : gravityLimit;
                Controller.verticalVelocity = math.clamp(Controller.verticalVelocity, gravityLowerLimit, gravityUpperLimit);

                return;
            }

            Controller.verticalVelocity += Controller.Data.gravity * GetVerticalVelocityMultiplier() * deltaTime;
        }

        void SlideOnSlope() {
            if (CollidingWithSlope && Controller.verticalVelocity < 0.0f) {
                Vector3 velocity = Controller.HorizontalVelocity + Vector3.up * Controller.verticalVelocity;
                Vector3 slopedVelocity = Vector3.ProjectOnPlane(velocity, Controller.hitNormal);

                Vector3 additionalHorizontalVelocity = slopedVelocity.ToHorizontal3() - Controller.HorizontalVelocity;
                float additionalVerticalVelocity = slopedVelocity.y - Controller.verticalVelocity;

                float slopeFriction = Controller.groundTouchedTimeout / GroundedData.groundTouchedTimeout;
                float velocityScale = math.clamp(1f - slopeFriction, 0f, 1f);
                
                Controller.additionalMoveVector += additionalHorizontalVelocity * velocityScale;
                Controller.verticalVelocity += additionalVerticalVelocity * velocityScale;
            }
        }

        float GetVerticalVelocityMultiplier() {
            // disable gravity when loading scene not to fall under it
            if (Hero.IsPortaling || Hero.AllowNpcTeleport || World.HasAny<LoadingScreenUI>()) {
                return 0;
            }

            if (Data.terminalVelocity >= Controller.verticalVelocity) {
                return 0;
            }

            if (Controller.verticalVelocity < 0 && FallingSpeed <= 0 && FallingSpeed > Controller.verticalVelocity * 0.9f) {
                return (FallingSpeed / Controller.verticalVelocity) * FallingSpeedMultiplier;
            }

            if (Controller.verticalVelocity < Data.fasterFallingThreshold) {
                return Data.fasterFallingMultiplier;
            }

            return FallingSpeed >= 0 ? 1 : FallingSpeedMultiplier;
        }

        void MinYCheck() {
            Vector3 position = Controller.Transform.position;
            if (position.y < VHeroController.MinPositionY) {
                Vector3 snapToGround = Ground.SnapToGround(position);
                if (Math.Abs(snapToGround.y - position.y) < 0.005f) {
                    Log.Important?.Error("Player was found outside of the map at: " + Controller.Transform.position + " on scene " + Controller.gameObject.scene.name);
                    Hero.TeleportTo(Portal.FindDefaultEntry().GetDestination());
                } else {
                    Hero.TeleportTo(snapToGround);
                }
            }
        }
        
        // === Sprinting Helpers
        void TrackSprintingState(bool sprinting) {
            if (IsSprinting && !sprinting) {
                IsSprinting = false;
                Hero.Trigger(Hero.Events.HeroSprintingStateChanged, false);
            } else if (!IsSprinting && sprinting) {
                IsSprinting = true;
                Hero.Trigger(Hero.Events.HeroSprintingStateChanged, true);
            }
        }

        void TrackWalkingState(bool walking) {
            if (_isWalking && !walking) {
                _isWalking = false;
                Hero.Trigger(Hero.Events.HeroWalkingStateChanged, false);
            } else if (!_isWalking && walking) {
                _isWalking = true;
                Hero.Trigger(Hero.Events.HeroWalkingStateChanged, true);
            }
        }
        
        bool SprintingRequirementsMet(Vector2 moveVector) {
            bool canBeInSprint = !IsSprinting ? CanStartSprinting : CanSprint;
            float minYInputForSprint = RewiredHelper.IsGamepad ? Data.minYInputForSprint : 0;
            return canBeInSprint && moveVector.y > minYInputForSprint;
        }
        
        
        // === Helpers
        void MakeMovementSound(Vector3 velocity, float deltaTime) {
            if (Controller.Grounded) {
                float noiseRange = velocity.magnitude;
                if (noiseRange > 0) {
                    noiseRange *= MoveSoundRangeMultiplier;
                    Vector3 headPosition = Controller.Head.position;
                    float noiseMultiplier = Controller.IsCrouching ? Hero.HeroStats.CrouchNoiseMultiplier : Hero.HeroStats.NoiseMultiplier;
                    float multipliers = Data.moveSoundMultiplier * noiseMultiplier;
                    float soundStrength;
                    
                    if (Controller.IsCrouching) {
                        float rangeModifier;
                        float theftStrength;
                        (rangeModifier, soundStrength, theftStrength) = ParentModel.Element<ArmorWeight>().ArmorNoiseStrength();
                        noiseRange *= rangeModifier;
                        ThieveryNoise.MakeNoise(noiseRange * TheftMoveSoundRangeMultiplier, theftStrength * multipliers, false, headPosition, Hero);
                    } else {
                        soundStrength = NoiseStrength.WalkingMovement;
                    }

                    AINoises.MakeNoiseOverTime(noiseRange, soundStrength * multipliers, deltaTime, false, headPosition, Hero);
                }
            }
        }

        bool CheckStaminaBeforeUse(float cost) {
            bool result = Stamina.ModifiedValue >= cost;
            if (!result) {
                Hero.Trigger(Hero.Events.StatUseFail, Stamina.Type);
            }

            return result;
        }
        
        // Animator
        void UpdateAnimator() {
            Controller.audioAnimator.SetFloat(Movement, Controller.Grounded || IsSwimming ? Controller.HorizontalSpeed : 0);
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop && Controller != null && Controller.audioAnimator != null) {
                Controller.audioAnimator.SetFloat(Movement, 0);
            }
            base.OnDiscard(fromDomainDrop);
        }
    }
}