using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Character.Features.Config;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Crosshair;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.PhysicsUtils;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Awaken.Utility.PhysicUtils;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using JetBrains.Annotations;
using Pathfinding.RVO;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.VFX;
using Sequence = DG.Tweening.Sequence;

namespace Awaken.TG.Main.Heroes.Combat {
    [UsesPrefab("Hero/VHeroController")]
    [RequireComponent(typeof(ARFmodEventEmitter))][Il2CppEagerStaticClassConstruction]
    public class VHeroController : View<Hero>, ICharacterView {
        static readonly int Crouching = Animator.StringToHash("Crouching");
        static readonly int IsSwimmingHash = Animator.StringToHash("IsSwimming");
        
        public const float MinPositionY = -250f;
        const float CircleToSquare = 0.71f;
        const float BowingCheckBoxHeight = 0.05f;
        const float BowingSpeed = 3f;
        const float SlowMotionOnDeathTime = 2.7f;

        [ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus")]
        public bool Grounded { get; set; } = true;

        [ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus")]
        public bool Encumbered => Target?.IsEncumbered ?? false;
        [ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus")]
        public bool StoryCrouched { get; private set; }

        //[ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus")]
        public HeroControllerData Data => Target.Data;
        public CharacterGroundedData GroundedData => Target.GroundedData;

        [FoldoutGroup("HeroBody"), PrefabAssetReference] public ARAssetReference maleHeroBodyFPP;
        [FoldoutGroup("HeroBody"), PrefabAssetReference] public ARAssetReference femaleHeroBodyFPP;
        [FoldoutGroup("HeroBody"), PrefabAssetReference] public ARAssetReference maleHeroBodyTPP;
        [FoldoutGroup("HeroBody"), PrefabAssetReference] public ARAssetReference femaleHeroBodyTPP;
        
        [FoldoutGroup("Cinemachine")] public GameObject fppParent;
        [FoldoutGroup("Cinemachine")] public GameObject tppParent;
        [FoldoutGroup("Cinemachine")] public GameObject tppPivot;
        [FoldoutGroup("Cinemachine")] public GameObject tppShoulderOffset;
        [FoldoutGroup("Cinemachine")] public GameObject tppDialogueOffset;
        [FoldoutGroup("Cinemachine")] public Transform hips;
        [FoldoutGroup("Cinemachine")] public CinemachineVirtualCamera baseVirtualCamera;
        [FoldoutGroup("Cinemachine")] public CinemachineVirtualCamera tppVirtualCamera;
        [FoldoutGroup("Cinemachine")] public CinemachineVirtualCamera dialogueVirtualCamera;
        [FoldoutGroup("Cinemachine")] public CinemachineVirtualCamera finisherVirtualCamera;
        [FoldoutGroup("Cinemachine")] public SphereCollider zoneCheckers;
        [FoldoutGroup("Cinemachine")] public CinemachineImpulseSource impulseSource;
        [FoldoutGroup("Cinemachine")] public Transform tppAimAssistTargetFollower;
        [FoldoutGroup("Cinemachine")] public Transform aimAssistTarget;

        [FoldoutGroup("RVO")] public RVOController rvoController;
        [FoldoutGroup("Animators")] public Animator audioAnimator;

        [FoldoutGroup("HeadCheck"), ReadOnly] public float headPositionRayOffset = 0.1f;
        [FoldoutGroup("HeadCheck"), ReadOnly] public bool headCollided = false;

        [FoldoutGroup("Height"), NonSerialized]
        public HeroControllerData.HeightData currentHeightData;

        [FoldoutGroup("Height"), NonSerialized]
        public float bowingCameraHeight;

        [FoldoutGroup("Height"), ShowInInspector]
        float _currentCameraHeight;

        [FoldoutGroup("Height"), ShowInInspector]
        bool _needUpdateHeightOneMoreTime;
        
        Transform _transform;
        
        [ShowInInspector, ReadOnly, FoldoutGroup("InternalStatus"), PropertyOrder(1000)]
        float _defaultFPPHeadZPosition, _oxygenUsageDelay;
        
        [NonSerialized, ReadOnly, FoldoutGroup("InternalStatus")]
        public Vector3 hitNormal,
                       additionalMoveVector,
                       slipFromAIVector;

        [NonSerialized, ReadOnly, FoldoutGroup("InternalStatus")]
        public float verticalVelocity,
                     groundTouchedTimeout, 
                     waterLeaveTimeout;
        
        [NonSerialized, ReadOnly, FoldoutGroup("InternalStatus")]
        public bool isSwimming,
                    isKicking,
                    groundDetected,
                    isSlippingFromAI;
        
        [NonSerialized, ReadOnly, FoldoutGroup("InternalStatus")]
        public VCHeroWaterChecker.HeroInWaterData currentWaterData;

        /// <summary> VHeroController transform.position in last LateUpdate </summary>
        public Vector3 LatePosition { get; private set; }
        
        /// <summary> VHeroController transform.rotation in last LateUpdate </summary>
        public Quaternion LateRotation { get; private set; }

        /// <summary> VHeroController transform.forward in last LateUpdate </summary>
        public Vector3 LateForward { get; private set; }

        public HeroCamera HeroCamera { get; private set; }

        Sequence _crouchTween;
        ARFmodEventEmitter _emitter;
        CharacterController _controller;
        CharacterControllerTweakedMovementHandler _movementHandler;
        Dictionary<string, LayerMask> _layerMaskOverrides = new();
        
        readonly List<LinkedEntityLifetime> _hiddenRenderers = new();
        readonly List<KandraRenderer> _hiddenKandraRenderers = new();
        readonly List<VisualEffect> _hiddenVfx = new();
        Quaternion _spineRotation, _spine2Rotation;
        Vector3 _spinePosition, _spine2Position;
        ScreenShakesReactiveSetting _screenShakesReactiveSetting;
        GameConstants _gameConstants;
        
        bool _crouchTweenActive;
        TimeScaleCache _timeScaleCache = new TimeScaleCache();

        Cinemachine3rdPersonFollow _cinemachine3RdPerson;
        float _original3rdPersonVerticalDamping;
        
        PositionConstraint _tppPivotPositionConstraint;
        RotationConstraint _tppPivotRotationConstraint;
        
        // --- Body Instance Components
        ARAsyncOperationHandle<GameObject> _heroBodyHandle;
        GameObject _heroBodyInstance;
        CancellationTokenSource _showHideTokenSource;

        public Vector3 LookDirection => MainCamera.transform.forward;
        public Vector3 CameraPosition => MainCamera.transform.position;
        
        public bool IsCharacter => true;
        public ICharacter Character => Target;

        public Vector3 HorizontalVelocity {
            get {
                var multiplier = _timeScaleCache.GetTimeScaleMultiplier(Time.frameCount);
                if (multiplier <= 0) {
                    return Vector3.zero;
                }
                var velocity = Controller.velocity;
                return new Vector3(velocity.x, 0, velocity.z) * multiplier;
            }
        }

        public Camera MainCamera => World.Only<CameraStateStack>().MainCamera;
        public PlayerInput Input => World.Any<PlayerInput>();
        public CharacterController Controller => _controller;
        public Transform Transform => _transform;

        [UnityEngine.Scripting.Preserve] public bool ShouldStand => Controller != null && !Target.IsCrouching && !isSwimming;
        public bool IsCrouching => Controller != null && Target.IsCrouching;
        public bool IsSwimming => Controller != null && isSwimming;
        public float HorizontalSpeed => HorizontalVelocity.magnitude;
        public bool IsSprinting => MovementSystem is HumanoidMovementBase {IsSprinting: true} && !IsPerformingAction && Grounded;
        public IEnumerable<Animator> HeroAnimators {
            get {
                yield return audioAnimator;
                yield return HeroAnimator;
            }
        }
        public bool CanStandUp => IsCrouching && !headCollided;
        public Vector2 ForcedInputFromCode => Target.TryGetElement<ForcedInputFromCode>()?.InputAcceleration ?? Vector2.zero;
        
        HeroMovementSystem MovementSystem => Target.MovementSystem;
        bool Mounted => Target.Mounted;
        bool StaminaRegenPrevented => Target.HasElement<IPreventStaminaRegen>();
        bool ManaRegenPrevented => Target.AnimatorSharedData?.MagicHeld ?? false;
        bool IsPerformingAction => MovementSystem is HumanoidMovementBase {IsPerformingAction: true} || Target.IsPerformingAction;
        
        LimitedStat Stamina => Target.Stamina;
        LimitedStat Mana => Target.Mana;
        LimitedStat Health => Target.Health;
        LimitedStat OxygenLevel => Target.HeroStats.OxygenLevel;
        
        bool PullingRangedWeapon => Target.PullingRangedWeapon;
        public Vector2 LocalVelocity => new(Vector3.Dot(LateForward, HorizontalVelocity), Vector3.Dot(LateRotation * Vector3.right, HorizontalVelocity));
        HeroControllerData.HeightData StandingHeight  => MovementSystem.StandingHeight;
        HeroControllerData.HeightData CrouchingHeight => MovementSystem.CrouchingHeight;
        float HeadCheckRayLength => MovementSystem.HeadCheckRayLength;
        
        // --- Body Instance Data
        public bool PerspectiveChangeInProgress { get; private set; }
        public HeroBodyData BodyData { get; private set; }
        public VCHeroRaycaster Raycaster { get; private set; }
        public Animator HeroAnimator { get; private set; }
        public ARHeroAnimancer Animancer { get; private set; }

        public GameObject CinemachineHeadTarget => BodyData == null ? null : BodyData.cinemachineHeadTarget;
        
        public Transform MainHand => BodyData.mainHand;
        public Transform OffHand => BodyData.offHand;
        public Transform MainHandWrist => BodyData.mainHandWrist;
        public Transform OffHandWrist  => BodyData.offHandWrist;
        public Transform Head => BodyData == null ? Transform : BodyData.head;
        public Transform Torso  => BodyData == null ? Transform : BodyData.torso;
        public Transform Hips  => BodyData == null || !Hero.TppActive ? hips : BodyData.hips;
        [UsedImplicitly, UnityEngine.Scripting.Preserve] // Used by VisualScripting
        public Transform FirePoint  => BodyData == null ? Transform : BodyData.firePoint;
        public Transform AimAssistTargetFollower  => BodyData == null ? Transform : BodyData.aimAssistTargetFollower;
        public Transform LeftElbow  => BodyData == null ? null : BodyData.leftElbow;
        public VFXBodyMarker VFXBodyMarker  => BodyData.vfxBodyMarker;
        
        AnimationCurve DeathSlowDownCurve => BodyData.deathSlowDownCurve;
        Transform MainHandParent => BodyData.mainHandParent;
        Transform OffHandParent => BodyData.offHandParent;
        Transform LeftLeg => BodyData.leftLeg;
        Transform RightLeg => BodyData.rightLeg;
        Transform Spine => BodyData.spine;
        Transform Spine2 => BodyData.spine2;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().DefaultForHero();

        void Awake() {
            _transform = transform;
        }

        protected override void OnInitialize() {
            AsyncOnInitialize().Forget();
        }

        async UniTaskVoid AsyncOnInitialize() {
            World.Only<GameCamera>().SetCinemachineCamera(Hero.TppActive ? tppVirtualCamera : baseVirtualCamera);
            _controller = GetComponent<CharacterController>();
            _movementHandler = GetComponent<CharacterControllerTweakedMovementHandler>();
            _emitter = GetComponent<ARFmodEventEmitter>();
            
            _movementHandler.SetGroundMask(RenderLayers.Mask.CharacterGround);
            _movementHandler.SetBaseSlopeLimit(GroundedData.slopeCriticalAngle);

            _defaultFPPHeadZPosition = fppParent.transform.localPosition.z;
            currentHeightData = Data.standingHeightData.Copy();
            _currentCameraHeight = currentHeightData.defaultCameraHeight;
            _needUpdateHeightOneMoreTime = true;

            await LoadBodyPrefab();
            await UniTask.WaitUntil(() => Input != null);
            InitListeners();
            
            HeroCamera = new HeroCamera(this);

            MovementSystem.Init(this);
            if (Target.IsCrouching) {
                CrouchNoChecks(0);
            }
            
            _spineRotation = Spine.localRotation;
            _spine2Rotation = Spine2.localRotation;
            _spinePosition = Spine.localPosition;
            _spine2Position = Spine2.localPosition;
            
            _screenShakesReactiveSetting = World.Only<ScreenShakesReactiveSetting>();
            _gameConstants = GameConstants.Get;

            _cinemachine3RdPerson = tppVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            _original3rdPersonVerticalDamping = _cinemachine3RdPerson.Damping.y;
            
            SetHeroPerspective(Hero.TppActive);
            Target.FoV.UpdateFoV();
        }

        void InitListeners() {
            Target.ListenTo(Hero.Events.Died, OnDeath, this);
            Target.ListenTo(Hero.Events.Revived, OnRevive, this);
            Target.ListenTo(HeroFoV.Events.FoVUpdated, SetFoV, this);
            Target.ListenTo(VCHeroWaterChecker.Events.WaterCollisionUpdate, OnWaterStateUpdate, this);
            Target.ListenTo(DirectionalCameraShakeSource.Events.InvokeShake, data => DirectionalCameraShake(data.position, data.force, data.impulseSource), this);
            
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, CompleteCrouchTween);
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.AfterHeroRested, this, time => UpdateStatsOnRest(time).Forget());
        }

        public void FinalInit() {
            Target.GetOrCreateTimeDependent()
                  .WithUpdate(ProcessUpdate)
                  .WithLateUpdate(ProcessLateUpdate)
                  .WithFixedUpdate(ProcessFixedUpdate)
                  .WithTimeScaleChanged(OnTimeScaleChanged)
                  .WithTimeComponentsOf(gameObject);
            
            _timeScaleCache.UpdateTimeScale(Target.GetTimeScaleModifier(), Time.frameCount);
        }

        // === Lifecycle
        void ProcessUpdate(float deltaTime) {
            if (BodyData == null) {
                return;
            }
            
            if (Target.IsAlive) {
                _controller.enableOverlapRecovery = HeroOverlapRecoveryHandler.CanRecoverFromOverlap();
                UpdateStats(deltaTime);
                MovementSystem.Update(deltaTime);
                UpdateHeight(deltaTime);
                UpdateRvoVelocity();
                ApplyTransformToTarget();
            }
            
            HeroCamera.CameraRotation(deltaTime);
        }

        void ProcessLateUpdate(float deltaTime) {
            if (BodyData == null) {
                return;
            }
            
            _transform.GetPositionAndRotation(out var position, out var rotation);
            LatePosition = position;
            LateRotation = rotation;
            LateForward = rotation * Vector3.forward;
        }

        void OnTimeScaleChanged(float from, float to) {
            if (_crouchTween is { active: true }) {
                _crouchTween.timeScale = to;
            }

            if (_fovTween is { active: true }) {
                _fovTween.timeScale = to;
            }
            
            _timeScaleCache.UpdateTimeScale(to, Time.frameCount);
        }
        
        void OnControllerColliderHit(ControllerColliderHit hit) {
            MovementSystem.OnControllerColliderHit(hit);
        }
        
        void OnDeath() {
            Controller.enabled = false;
            StartSlowDown();
            SetWeaponHold(false);
            SetLegsVisibility(false);
        }
        
        void StartSlowDown() {
            SlowDownTime.SlowTime(new TimeDuration(SlowMotionOnDeathTime, true), DeathSlowDownCurve);
        }

        void OnRevive() {
            SetWeaponHold(true);
            SetLegsVisibility(true);
            Controller.enabled = true;
           
            Spine.SetLocalPositionAndRotation(_spinePosition, _spineRotation);
            Spine2.SetLocalPositionAndRotation(_spine2Position, _spine2Rotation);
        }
        
        void SetWeaponHold(bool areWeaponsHeld) {
            if (areWeaponsHeld) {
                Destroy(MainHand.GetComponent<Rigidbody>());
                Destroy(MainHand.GetComponent<SphereCollider>());
                MainHand.SetParent(MainHandParent);
                MainHand.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                MainHand.localScale = Vector3.one;
                Destroy(OffHand.GetComponent<Rigidbody>());
                Destroy(OffHand.GetComponent<SphereCollider>());
                OffHand.SetParent(OffHandParent);
                OffHand.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                OffHand.localScale = Vector3.one;
            } else {
                MainHand.parent = null;
                MainHand.AddComponent<Rigidbody>().mass = 10f;
                MainHand.AddComponent<SphereCollider>().radius = 0.25f;
                OffHand.parent = null;
                OffHand.AddComponent<Rigidbody>().mass = 10f;
                OffHand.AddComponent<SphereCollider>().radius = 0.25f;
            }
        }
        
        void SetLegsVisibility(bool areLegsVisible) {
            if (Hero.TppActive) {
                return;
            }
            
            LeftLeg.localScale = areLegsVisible ? Vector3.one : Vector3.zero;
            RightLeg.localScale = areLegsVisible ? Vector3.one : Vector3.zero;
        }

        void ProcessFixedUpdate(float deltaTime) {
            //Update graphs
            // DebugGUI.Graph(nameof(_verticalVelocity), _verticalVelocity);
            // DebugGUI.Graph(nameof(HorizontalVelocity), HorizontalVelocity.magnitude);
            //
            // DebugGUI.Graph("Forward velocity", LocalVelocity.x);
            // DebugGUI.Graph("Sideways velocity", LocalVelocity.y);

            MovementSystem.FixedUpdate(deltaTime);
        }

        // === Movement
        public void PerformGroundChecks(float deltaTime) {
            DetectGround();
            FetchHitNormal();
            UpdateGroundTouchedTimeout(deltaTime);
            
            var wasGrounded = Grounded;
            
            Grounded = groundDetected && groundTouchedTimeout > 0f;
            
            if (!wasGrounded && Grounded) {
                CheckForGroundDamageNullifier();
                Target.Trigger(Hero.Events.HeroLanded, verticalVelocity);
            }
        }
        
        void DetectGround() {
            var spherePosition = _transform.position + Vector3.down * GroundedData.groundedOffset;
            groundDetected = Physics.CheckSphere(spherePosition, GroundedData.groundedRadius, RenderLayers.Mask.CharacterGround, QueryTriggerInteraction.Ignore);
        }

        void FetchHitNormal() {
            hitNormal = _movementHandler.Grounded ? _movementHandler.GroundNormal : _movementHandler.HitNormal;
            if (hitNormal.y < 0f) {
                hitNormal = Vector3.zero;
            }
        }

        void UpdateGroundTouchedTimeout(float deltaTime) {
            if (MovementSystem is HeroKnockdownMovement) {
                ForceGroundTouchedTimeout();
            } else if (_movementHandler.Grounded) {
                groundTouchedTimeout = GroundedData.groundTouchedTimeout;
            } else if (verticalVelocity > 0f) {
                groundTouchedTimeout = 0f;
            } else if (groundTouchedTimeout > 0f) {
                groundTouchedTimeout -= deltaTime;
            }
        }

        public void ForceGroundTouchedTimeout() {
            groundTouchedTimeout = GroundedData.groundTouchedTimeout;
        }
        
        void CheckForGroundDamageNullifier() {
            var spherePosition = _transform.position + Vector3.down * GroundedData.groundedOffset;
            var hitFallDamageNullifier = PhysicsQueries.OverlapSphere(spherePosition, GroundedData.groundedRadius, RenderLayers.Mask.FallDamageNullifier, QueryTriggerInteraction.Collide);

            foreach (var damageNullifierCollider in hitFallDamageNullifier) {
                if (damageNullifierCollider.TryGetComponentInParent(out HeroFallDamageNullifier marker)) {
                    Target.Element<HeroFallDamage>().FallDamageNullified(marker.SurfaceType);
                    break;
                }
            }
        }
        
        public void PerformWaterCheck(float deltaTime) {
            bool wasSwimming = isSwimming;
            bool offsetSatisfied = false;

            if (waterLeaveTimeout > 0) {
                waterLeaveTimeout -= deltaTime;
            }

            if (currentWaterData.hasRaycastHit) {
                offsetSatisfied = wasSwimming 
                    ? currentWaterData.distanceToWaterSurface > Data.swimmingOffset - 0.1f 
                    : currentWaterData.distanceToWaterSurface > Data.swimmingOffset;
            }

            if (currentWaterData.hasRaycastHit && offsetSatisfied || Target.IsUnderWater) {
                isSwimming = true;
                if (!wasSwimming) {
                    verticalVelocity = 0;
                }
            } else {
                isSwimming = false;
            }

            bool startedSwimming = !wasSwimming && isSwimming;
            bool stoppedSwimming = wasSwimming && !isSwimming;

            if (startedSwimming) {
                Target.Trigger(Hero.Events.HeroSwimmingStateChanged, true);
                Target.Trigger(Hero.Events.HideWeapons, true);
            } else if (stoppedSwimming) {
                Target.Trigger(Hero.Events.HeroSwimmingStateChanged, false);
            }

            audioAnimator.SetBool(IsSwimmingHash, isSwimming);
            
            if (startedSwimming) {
                if (IsCrouching) {
                    ToggleCrouch(0, false);
                }

                SwimCrouch(true);
            } else if (stoppedSwimming) {
                SwimCrouch(false);
                waterLeaveTimeout = Data.waterLeaveTimeout;
            }
        }
        
        void OnWaterStateUpdate(VCHeroWaterChecker.HeroInWaterData data) {
            currentWaterData = data;
        }
        
        public void ApplyTransformToTarget() {
            Target.MoveTo(_transform.position);
            Target.Rotation = _transform.rotation;
        }

        public void CancelTppCameraDamping() {
            if (Hero.TppActive) {
                tppVirtualCamera.CancelDamping();
            }
        }

        public void SetActiveTppCameraVerticalDamping(bool enable) {
            Vector3 damping = _cinemachine3RdPerson.Damping;
            damping.y = enable ? _original3rdPersonVerticalDamping : 0;
            _cinemachine3RdPerson.Damping = damping;
        }
        
        public void GetHeadCheckParams(out Vector3 headCollisionCenterPosition, out Vector3 headCollisionHalfExtents, out float bowingMinCameraHeight,
                                       out Vector3 bowingCheckStart, out Vector3 bowingCheckHalfExtents, out float bowingCheckDistance) {
            var position = _transform.position;
            var radius = _controller.radius * CircleToSquare;

            var headCheckRayHalfLength = (HeadCheckRayLength + headPositionRayOffset) * 0.5f;
            var startRayPosition = new Vector3(position.x, Controller.bounds.max.y - headPositionRayOffset, position.z);
            headCollisionCenterPosition = startRayPosition + Vector3.up * headCheckRayHalfLength;
            headCollisionHalfExtents = new Vector3(radius, headCheckRayHalfLength, radius);

            var minCameraPosition = currentHeightData.bowingCameraHeight - BowingCheckBoxHeight * 1.5f;
            var maxCameraPosition = currentHeightData.defaultCameraHeight + currentHeightData.bowingCheckAdditionalLength;
            var bowCheckRadius = radius * 0.8f;
            bowingMinCameraHeight = minCameraPosition;
            bowingCheckStart = position + new Vector3(0, minCameraPosition, 0);
            bowingCheckHalfExtents = new Vector3(bowCheckRadius, BowingCheckBoxHeight * 0.5f, bowCheckRadius);
            bowingCheckDistance = maxCameraPosition - minCameraPosition;
        }

        public void DirectionalCameraShakeFromBeingHit(Vector3 sourcePosition, float damageAmount) {
            float hpPercent = damageAmount / Target.Health.ModifiedValue;
            DirectionalCameraShake(sourcePosition, hpPercent);
        }

        public void DirectionalCameraShake(Vector3 sourcePosition, float strengthPercent, CinemachineImpulseSource customSource = null) {
            if (MovementSystem is HeroKnockdownMovement || !_screenShakesReactiveSetting.Enabled) {
                return;
            }
            
            Vector3 direction = sourcePosition - _transform.position;
            float force = Mathf.Lerp(_gameConstants.minDirectionalShakesStrength, _gameConstants.maxDirectionalShakesStrength, strengthPercent / _gameConstants.directionalShakesHealthCutoff);
            Vector3 velocity = direction.normalized * force;
            var currentSource = customSource != null ? customSource : impulseSource;
            currentSource.GenerateImpulseAtPositionWithVelocity(sourcePosition, velocity);
        }

        // === External Overriding

        Sequence _fovTween;
        Sequence _fovTppTween;

        public void SetFoV(HeroFoV.FoVChangeData data) {
            SetVirtualCameraFoV(data, baseVirtualCamera, ref _fovTween);
            SetVirtualCameraFoV(data, tppVirtualCamera, ref _fovTppTween);
        }

        void SetVirtualCameraFoV(HeroFoV.FoVChangeData data, CinemachineVirtualCamera virtualCamera, ref Sequence fovTween) {
            fovTween.Kill();
            if (Math.Abs(data.newFoV - virtualCamera.m_Lens.FieldOfView) < 0.01f) {
                return;
            }

            if (data.changeLength < 0.01f) {
                virtualCamera.m_Lens.FieldOfView = data.newFoV;
            } else {
                fovTween = DOTween.Sequence();
                fovTween.Append(DOTween.To(() => virtualCamera.m_Lens.FieldOfView, f => virtualCamera.m_Lens.FieldOfView = f, data.newFoV, data.changeLength));
                fovTween.timeScale = this.GetTimeScaleModifier();
            }
        }
        
        public void SetVerticalVelocity(float value) {
            verticalVelocity = value;
        }
        
        public void MoveTowards(Vector3 direction) {
            additionalMoveVector += direction;
        }
        
        // === Crouching
        public void ToggleCrouch(float duration = 0.5f, bool? forceTo = null) {
            if (_crouchTweenActive && forceTo == null) return;
            if (IsCrouching && !CanStandUp) return;

            Target.IsCrouching = forceTo ?? !Target.IsCrouching;

            CrouchNoChecks(duration);
        }

        void CrouchNoChecks(float duration) {
            CrouchInternal(Target.IsCrouching ? CrouchingHeight : StandingHeight, duration);

            Target.Trigger(Hero.Events.HeroCrouchToggled, Target.IsCrouching);
            audioAnimator.SetBool(Crouching, Target.IsCrouching);
        }

        public void StoryBasedCrouch(bool activate, float duration = 0.5f) {
            if (!activate && headCollided) return;
            StoryCrouched = activate;
            CrouchInternal(activate ? CrouchingHeight : StandingHeight, duration);
        }

        void SwimCrouch(bool activate, float duration = 0.5f) {
            CrouchInternal(activate ? Data.swimmingHeightData : StandingHeight, duration);
        }

        public void GlidingCrouch(bool activate, float duration = 0.5f) {
            var heightDataToReturnTo = IsSwimming ? Data.swimmingHeightData : StandingHeight;
            CrouchInternal(activate ? Data.glidingHeightData : heightDataToReturnTo, duration);
        }

        void CrouchInternal(in HeroControllerData.HeightData data, float duration) {
            _crouchTween.Kill();
            if (duration == 0) {
                currentHeightData.SetTo(data);
                _crouchTweenActive = false;
                return;
            }
            _crouchTweenActive = true;
            _crouchTween = currentHeightData.TweenTo(data, duration)
                .OnComplete(() => _crouchTweenActive = false)
                .OnKill(() => _crouchTweenActive = false);
            _crouchTween.timeScale = this.GetTimeScaleModifier();
        }

        void CompleteCrouchTween() {
            if (_crouchTweenActive) {
                _crouchTween.Complete();
            }
        }
        
        void UpdateHeight(float deltaTime) {
            var desiredCameraHeight = Mathf.Min(bowingCameraHeight, currentHeightData.defaultCameraHeight);
            if (Math.Abs(_currentCameraHeight - desiredCameraHeight) > 0.001f) {
                _currentCameraHeight = Mathf.MoveTowards(_currentCameraHeight, desiredCameraHeight, deltaTime * BowingSpeed);
            } else if (_crouchTweenActive) {
                _needUpdateHeightOneMoreTime = true;
            } else if (_needUpdateHeightOneMoreTime) {
                _needUpdateHeightOneMoreTime = false;
            } else {
                // if we get there it means nothing could possibly change any height parameter
                return;
            }

            SetCameraHeight();
        }

        void SetCameraHeight() {
            _controller.height = currentHeightData.height;
            _controller.center = new Vector3(0, currentHeightData.height * 0.5f - currentHeightData.groundSubmerging);
            fppParent.transform.localPosition = new Vector3(0, _currentCameraHeight, _defaultFPPHeadZPosition);
            tppPivot.transform.localPosition = new Vector3(0, _currentCameraHeight, 0);
            zoneCheckers.center = new Vector3(0, currentHeightData.zoneCheckerHeight, 0);
        }

        void UpdateRvoVelocity() {
            // Override the RVOController's velocity. This will disable local avoidance calculations for one simulation step.
            rvoController.velocity = Controller.velocity;
        }

        void UpdateStats(float deltaTime) {
            bool mountedOrGroundedOrSwimming = Mounted || Grounded || isSwimming;
            bool isNotPerformingAction = !IsSprinting && !Target.IsBlocking && !PullingRangedWeapon;
            if (!Stamina.IsMaxFloat && mountedOrGroundedOrSwimming && isNotPerformingAction && !StaminaRegenPrevented) {
                Stamina.IncreaseBy(Target.StaminaRegen.ModifiedValue * deltaTime);
            }

            float manaRegen = Target.ManaRegen;
            bool shouldChangeMana = (manaRegen > 0 && !Mana.IsMaxFloat) || (manaRegen < 0 && !Mana.IsMinFloat);
            if (shouldChangeMana && !ManaRegenPrevented) {
                Mana.IncreaseBy(Target.ManaRegen * deltaTime);
            }
            
            if (!Health.IsMaxFloat) {
                HealingUtils.TakeHealing(Target, Target.HealthRegen.ModifiedValue * deltaTime);
            }

            if (Target.IsUnderWater) {
                if (_oxygenUsageDelay >= Data.oxygenUsageDelay) {
                    OxygenLevel.DecreaseBy(Target.HeroStats.OxygenUsage * deltaTime);
                } else {
                    _oxygenUsageDelay += deltaTime;
                }
            } else if (OxygenLevel.Percentage < 1) {
                _oxygenUsageDelay = 0;
                OxygenLevel.IncreaseBy(Data.oxygenRegenPerTick * deltaTime);
            }
        }
        
        async UniTaskVoid UpdateStatsOnRest(int gameTimeInMinutes) {
            if (await AsyncUtil.DelayFrame(this)) {
                float realTimeChangeInSeconds = gameTimeInMinutes * 60f / World.Only<GameRealTime>().WeatherSecondsPerRealSecond;
                HealingUtils.TakeHealing(Target, Target.HealthRegen.ModifiedValue * realTimeChangeInSeconds);
                Mana.IncreaseBy(Target.ManaRegen * realTimeChangeInSeconds);
                Stamina.IncreaseBy(Target.StaminaRegen.ModifiedValue * realTimeChangeInSeconds);
            }
        }
        
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot, GameObject followObject = null, params FMODParameter[] eventParams) {
            EventReference eventRef = audioType.RetrieveFrom(Character);
            PlayAudioClip(eventRef, asOneShot, followObject, eventParams);
        }

        public void PlayAudioClip(EventReference eventReference, bool asOneShot, GameObject followObject = null, params FMODParameter[] eventParams) {
            if (asOneShot) {
                if (BodyData == null) {
                    Target.OnVisualLoaded(() => PlayAudioClip(eventReference, true, followObject, eventParams));
                    return;
                }
                if (followObject == null) {
                    followObject = CinemachineHeadTarget.gameObject;
                }
                FMODManager.PlayAttachedOneShotWithParameters(eventReference, followObject, this, eventParams);
            } else {
                //_emitter.PlayNewEventWithPauseTracking(eventReference, eventParams);
            }
        }
        
        // === Hero Combat actions
        
        public async UniTaskVoid KickBegun() {
            isKicking = true;
            await UniTask.Delay(1260);
            isKicking = false;
        }
        
        readonly RaycastHit[] _hitResults = new RaycastHit[32];

        public Vector3 Pommel(Item statsItem) {
            var hitMask = Data.enemiesHitMask;
            Vector3 firePointForward = FirePoint.forward;
            int hits = Physics.BoxCastNonAlloc(FirePoint.position, Data.pushColliderSize, firePointForward, _hitResults, FirePoint.rotation, RaycastCheck.MinPhysicsCastDistance, hitMask);
            List<IAlive> pommeled = new();
            for (int i = 0; i < hits; i++) {
                Damage.DetermineTargetHit(_hitResults[i].collider, out IAlive receiver, out HealthElement healthElement);
                if (receiver == null || healthElement == null || pommeled.Contains(receiver)) {
                    continue;
                }

                DamageParameters parameters = DamageParameters.Default;
                parameters.Direction = firePointForward;
                parameters.ForceDirection = firePointForward;
                parameters.ForceDamage = statsItem.ItemStats.ForceDamage * statsItem.ItemStats.ForceDamagePushMultiplier;
                parameters.RagdollForce = statsItem.ItemStats.RagdollForce;
                parameters.PoiseDamage = statsItem.ItemStats.PoiseDamage * statsItem.ItemStats.PoiseDamagePushMultiplier;
                parameters.IsPush = true;
                Damage damage = Damage.CalculateDamageDealt(Target, receiver, parameters, Target.BlockRelatedStats.ParentModel).WithHitCollider(_hitResults[i].collider);
                damage.RawData.MultiplyMultModifier(Target.BlockRelatedStats.PushDamageMultiplier);
                healthElement.TakeDamage(damage);
                RewiredHelper.VibrateHighFreq(VibrationStrength.VeryStrong, VibrationDuration.Short);
                pommeled.Add(receiver);
            }

            pommeled.Clear();
            Stamina.DecreaseBy(Target.BlockRelatedStats.PushStaminaCost * Target.HeroStats.ItemStaminaCostMultiplier);
            var attackParameters = new AttackParameters(Character, Target.BlockRelatedStats.ParentModel, AttackType.Pommel, firePointForward);
            Character.Trigger(ICharacter.Events.OnAttackStart, attackParameters);
            Target.Trigger(Hero.Events.AfterHeroPommel, true);
            return firePointForward;
        }

        public void BackStab(Item itemDealingDamage) {
            VCHeroRaycaster vHeroRaycaster = Target.VHeroController.Raycaster;
            if (!vHeroRaycaster.NPCRef.TryGet(out Location location) || !location.TryGetElement(out NpcElement npcInFrontOfHero)) {
                return;
            }

            PreventBackStab.Prevent(Target, new TimeDuration(5));

            Vector3 firePointForward = FirePoint.forward;
            DamageParameters parameters = DamageParameters.Default;
            parameters.Direction = firePointForward;
            parameters.ForceDirection = firePointForward;
            parameters.Position = vHeroRaycaster.NpcCollider.transform.position;
            parameters.ForceDamage = itemDealingDamage.ItemStats.ForceDamage;
            parameters.RagdollForce = itemDealingDamage.ItemStats.RagdollForce;
            parameters.IsPush = false;
            parameters.IsBackStab = true;
            Damage damage = Damage.CalculateDamageDealt(Target, npcInFrontOfHero, parameters, itemDealingDamage).WithHitCollider(vHeroRaycaster.NpcCollider).WithItem(itemDealingDamage);

            Target.Trigger(Hero.Events.AfterHeroBackStab, true);
            npcInFrontOfHero.HealthElement.TakeDamage(damage);

            var attackParameters = new AttackParameters(Character, itemDealingDamage, AttackType.Normal, firePointForward);
            Character.Trigger(ICharacter.Events.OnAttackStart, attackParameters);
        }

        public void CastingBegun(CastingHand hand) {
            var equipmentType = hand switch {
                CastingHand.MainHand => EquipmentSlotType.MainHand,
                CastingHand.OffHand => EquipmentSlotType.OffHand,
                CastingHand.BothHands => EquipmentSlotType.MainHand,
                _ => throw new ArgumentOutOfRangeException(nameof(hand), hand, null),
            };
            Item castingItem = Target.HeroItems.EquippedItem(equipmentType);
            castingItem?.StartPerforming(ItemActionType.CastSpell);
            Target.Trigger(ICharacter.Events.CastingBegun, new CastSpellData { CastingHand = hand, Item = castingItem });
        }

        public void CastingCanceled(CastingHand hand) {
            var equipmentType = hand switch {
                CastingHand.MainHand => EquipmentSlotType.MainHand,
                CastingHand.OffHand => EquipmentSlotType.OffHand,
                CastingHand.BothHands => EquipmentSlotType.MainHand,
                _ => throw new ArgumentOutOfRangeException(nameof(hand), hand, null),
            };
            Item castingItem = Target.HeroItems.EquippedItem(equipmentType);
            CastingCanceled(hand, castingItem);
        }

        public void CastingCanceled(CastingHand hand, Item castingItem, bool triggerEvents = true) {
            castingItem?.CancelPerforming(ItemActionType.CastSpell);
            if (triggerEvents) {
                Target.Trigger(ICharacter.Events.CastingCanceled, new CastSpellData { CastingHand = hand, Item = castingItem });
            }
        }

        public void CastingEnded(CastingHand hand) {
            var equipmentType = hand switch {
                CastingHand.MainHand => EquipmentSlotType.MainHand,
                CastingHand.OffHand => EquipmentSlotType.OffHand,
                CastingHand.BothHands => EquipmentSlotType.MainHand,
                _ => throw new ArgumentOutOfRangeException(nameof(hand), hand, null),
            };
            Item castingItem = Target.HeroItems.EquippedItem(equipmentType);
            castingItem?.EndPerforming(ItemActionType.CastSpell);
            Target.Trigger(ICharacter.Events.CastingEnded, new CastSpellData { CastingHand = hand, Item = castingItem });
        }

        public void PerformMoveStep(Vector3 moveDelta) {
            if (moveDelta.IsInvalid()) {
#if UNITY_EDITOR || AR_DEBUG
                Log.Critical?.ErrorThenLogs($"Attempted hero movement with invalid move delta: {moveDelta}", Log.Utils.HeroMovementInvalid);
#endif
                moveDelta = Vector3.zero;
            }
            _movementHandler.PerformMoveStep(moveDelta);
        }

        public void OnHeroJumped() {
            _movementHandler.OnHeroJumped();
        }
        
        // === Helpers
        public void Show() {
            _showHideTokenSource?.Cancel();
            _showHideTokenSource = new CancellationTokenSource();
            if (BodyData == null) {
                PerformActionAfterBodyLoad(Show, _showHideTokenSource.Token).Forget();
                return;
            }
            
            ToggleHeroHands(true);
            
            foreach (var hiddenDrakeRenderer in _hiddenRenderers) {
                if (hiddenDrakeRenderer != null) {
                    hiddenDrakeRenderer.enabled = true;
                }
            }
            foreach (var hiddenKandraRenderer in _hiddenKandraRenderers) {
                if (hiddenKandraRenderer != null) {
                    hiddenKandraRenderer.enabled = true;
                }
            }
            foreach (var hiddenVfx in _hiddenVfx) {
                if (hiddenVfx != null) {
                    hiddenVfx.enabled = true;
                }
            }

            _hiddenRenderers.Clear();
            _hiddenKandraRenderers.Clear();
            _hiddenVfx.Clear();
            HeroCamera.ResetCameraPitch();

            ForceGroundTouchedTimeout();
            Target.BodyFeatures()?.RefreshCover();
        }

        public void Hide() {
            _showHideTokenSource?.Cancel();
            _showHideTokenSource = new CancellationTokenSource();
            if (BodyData == null) {
                PerformActionAfterBodyLoad(Hide, _showHideTokenSource.Token).Forget();
                return;
            }

            GetComponentsInChildren<LinkedEntityLifetime>(true, _hiddenRenderers);
            GetComponentsInChildren<KandraRenderer>(true, _hiddenKandraRenderers);
            GetComponentsInChildren<VisualEffect>(true, _hiddenVfx);
            
            foreach (var drakeRenderer in _hiddenRenderers) {
                drakeRenderer.enabled = false;
            }
            foreach (var kandraRenderer in _hiddenKandraRenderers) {
                kandraRenderer.enabled = false;
            }
            foreach (var vfx in _hiddenVfx) {
                vfx.enabled = false;
            }

            ToggleHeroHands(false);
        }

        void ToggleHeroHands(bool active) {
            MainHand.TrySetActiveOptimized(active);
            OffHand.TrySetActiveOptimized(active);
            MainHandWrist.TrySetActiveOptimized(active);
            OffHandWrist.TrySetActiveOptimized(active);
        }
        
        public void SetExcludedLayerMaskOverride(string owner, bool canPenetrate, LayerMask layerMaskOverride) {
            if (canPenetrate) {
                _layerMaskOverrides.Add(owner, layerMaskOverride);
            } else {
                _layerMaskOverrides.Remove(owner);
            }
            
            UpdateExcludedLayerMaskOverride();
        }

        void UpdateExcludedLayerMaskOverride() {
            LayerMask newLayerMaskOverride = 0;
            
            foreach (var layerMask in _layerMaskOverrides) {
                newLayerMaskOverride |= layerMask.Value;
            }

            _controller.excludeLayers = newLayerMaskOverride;
        }
        
        public async UniTaskVoid ChangeHeroPerspective(bool tppActive) {
            if (PerspectiveChangeInProgress) {
                return;
            }
            PerspectiveChangeInProgress = true;
            
            var saveBlocker = World.Add(new SaveBlocker(Target));

            if (Time.timeScale == 0) {
                // Wait until time is unpaused (otherwise head of the hero would be loaded around camera position in Pause Menu). 
                if (!await AsyncUtil.WaitUntil(this, () => Time.timeScale > 0)) {
                    PerspectiveChangeInProgress = false;
                    saveBlocker?.Discard();
                    return;
                }
            }
            
            // Ensure settings are updated
            Hero.TppActive = tppActive;
            var perspectiveSetting = World.Any<PerspectiveSetting>();
            if (perspectiveSetting != null) {
                perspectiveSetting.IsTPP = tppActive;
            }
            
            if (!await TryReloadBodyWithEquips()) {
                PerspectiveChangeInProgress = false;
                saveBlocker?.Discard();
                return;
            }
            
            SetHeroPerspective(tppActive);

            saveBlocker?.Discard();
            Target.Trigger(Hero.Events.HeroPerspectiveChanged, tppActive);
            
            await AsyncUtil.DelayFrameOrTime(this, 60, 2000);
            Target.FoV.UpdateFoV();
            PerspectiveChangeInProgress = false;
        }

        void SetHeroPerspective(bool tppActive) {
            HeroCamera.ChangeHeroPerspective(tppActive);
            Target.Element<HeroCrosshair>().HeroPerspectiveChanged(tppActive);
            Target.TryGetElement<HeroOffHandCutOff>()?.HeroPerspectiveChanged(tppActive);
        }
        
        // === Helpers
        public async UniTask LoadBodyPrefab() {
            BodyData = null;
            
            foreach (var fsm in Target.Elements<HeroAnimatorSubstateMachine>().Reverse()) {
                fsm.Discard();
            }
            
            BodyFeatures bodyFeatures = Target.TryGetElement<BodyFeatures>();
            if (bodyFeatures && _heroBodyInstance && _heroBodyInstance.TryGetComponent(out CharacterDefaultClothes clothes)) {
                clothes.RemoveFrom(bodyFeatures);
            }
            bodyFeatures?.Hide();

            ReleaseBodyInstance();
            
            bool isFemale = Target.GetGender() == Gender.Female;
            if (Hero.TppActive) {
                _heroBodyHandle = isFemale ? femaleHeroBodyTPP.LoadAsset<GameObject>() : maleHeroBodyTPP.LoadAsset<GameObject>();
            } else {
                _heroBodyHandle = isFemale ? femaleHeroBodyFPP.LoadAsset<GameObject>() : maleHeroBodyFPP.LoadAsset<GameObject>();
            }

            GameObject bodyPrefab = await _heroBodyHandle;
            if (bodyPrefab == null) {
                return;
            }
            _heroBodyInstance = GameObject.Instantiate(bodyPrefab, Hero.TppActive ? tppParent.transform : fppParent.transform);
            InitializeViewComponents(_heroBodyInstance.transform);
            
            BodyData = _heroBodyInstance.GetComponent<HeroBodyData>();
            HeroAnimator = _heroBodyInstance.GetComponentInChildren<Animator>();
            Animancer = _heroBodyInstance.GetComponentInChildren<ARHeroAnimancer>();
            Raycaster = _heroBodyInstance.GetComponentInChildren<VCHeroRaycaster>();
            
            if (!Hero.TppActive) {
                SetupFppBodyData();
            } else {
                SetupTppBodyData();
            }

            Target.TryGetElement<HeroRagdollElement>()?.Discard();
            Target.AddElement(new HeroRagdollElement());
            
            Target.InitializeAnimatorElements(HeroAnimator, Animancer);

            BodyFeatures features = Target.TryGetElement<BodyFeatures>();
            if (features && _heroBodyInstance.TryGetComponent(out clothes)) {
                clothes.AddTo(features, true).Forget();
            }

            if (Hero.TppActive && features != null) {
                features.InitCovers(Target.Element<HeroBodyClothes>());
                features.RefreshDistanceBand(0);
            }
            SetupTppPivotParentConstraint();

            features?.HeroPerspectiveChanged();
            if (Target.IsFullyInitialized) {
                features?.Show();
            }

            if (Target.Mounted) {
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            SetCameraHeight();
            Target.GetOrCreateTimeDependent()?.WithTimeComponentsOf(_heroBodyInstance);
            Target.VisualLoaded();
        }
        
        public async UniTask<bool> TryReloadBodyWithEquips() {
            // Weapon UnEquip Toggles Fist Equip and that needs to wait for new body to load.
            // That is why we set BodyData to null before unEquipping any item.
            BodyData = null;
            
            // Armor UnEquip
            List<(Item, EquipmentSlotType)> unEquippedItems = new();
            foreach (var slotType in EquipmentSlotType.Armors) {
                var item = Target.HeroItems.Unequip(slotType);
                if (item != null) {
                    unEquippedItems.Add((item, slotType));
                }
            }

            // Weapons UnEquip
            foreach (var slotType in EquipmentSlotType.Loadouts) {
                var item = Target.HeroItems.EquippedItem(slotType);
                if (item == null || item.HasElement<LockItemSlot>()) {
                    continue;
                }
                Target.HeroItems.Unequip(item);
                if (!item.IsFists) {
                    unEquippedItems.Add((item, slotType));
                }
            }
            
            Target.HeroItems.LockEquipping(true);
            // Destroy old body and load new one
            await LoadBodyPrefab();
            
            if (!await AsyncUtil.DelayFrame(this)) {
                return false;
            }
            
            // Weapon Equip
            Target.HeroItems.LockEquipping(false);
            foreach ((Item item, EquipmentSlotType slotType) in unEquippedItems) {
                Target.HeroItems.Equip(item, slotType);
            }

            return true;
        }

        void SetupFppBodyData() {
            baseVirtualCamera.Follow = BodyData.cinemachineHeadTarget.transform;
            dialogueVirtualCamera.Follow = BodyData.cinemachineHeadTarget.transform;
            finisherVirtualCamera.Follow = BodyData.cinemachineHeadTarget.transform;
        }

        void SetupTppBodyData() {
            var rotationConstraint = BodyData.firePoint.AddComponent<RotationConstraint>();
            rotationConstraint.AddSource(new ConstraintSource { sourceTransform = tppPivot.transform, weight = 1});
            rotationConstraint.weight = 1;
            rotationConstraint.constraintActive = true;
            rotationConstraint.rotationAxis = Axis.X;
            
            var positionConstraint = BodyData.firePoint.AddComponent<PositionConstraint>();
            positionConstraint.AddSource(new ConstraintSource { sourceTransform = tppPivot.transform, weight = 1});
            positionConstraint.weight = 1;
            positionConstraint.constraintActive = true;
            positionConstraint.translationAxis = Axis.Y;
            
            dialogueVirtualCamera.Follow = tppDialogueOffset.transform;
            finisherVirtualCamera.Follow = tppShoulderOffset.transform;
        }

        void SetupTppPivotParentConstraint() {
            if (Hero.TppActive) {
                if (_tppPivotPositionConstraint == null) {
                    _tppPivotPositionConstraint = tppPivot.AddComponent<PositionConstraint>();
                }

                if (_tppPivotRotationConstraint == null) {
                    _tppPivotRotationConstraint = tppPivot.AddComponent<RotationConstraint>();
                }

                var constraint = new ConstraintSource {
                    sourceTransform = BodyData.tppPivot,
                    weight = 1
                };

                if (_tppPivotPositionConstraint.sourceCount <= 0) {
                    _tppPivotPositionConstraint.AddSource(constraint);
                } else {
                    _tppPivotPositionConstraint.SetSource(0, constraint);
                }

                if (_tppPivotRotationConstraint.sourceCount <= 0) {
                    _tppPivotRotationConstraint.AddSource(constraint);
                } else {
                    _tppPivotRotationConstraint.SetSource(0, constraint);
                }

                _tppPivotPositionConstraint.constraintActive = true;
                _tppPivotPositionConstraint.translationAxis = Axis.X | Axis.Z;
                _tppPivotPositionConstraint.weight = 1;
                
                _tppPivotRotationConstraint.constraintActive = true;
                UpdateRotationConstraint(Target.Mounted);
                _tppPivotRotationConstraint.weight = 1;
            } else {
                Destroy(_tppPivotRotationConstraint);
                _tppPivotRotationConstraint = null;
                
                Destroy(_tppPivotPositionConstraint);
                _tppPivotPositionConstraint = null;
            }
        }

        public void UpdateRotationConstraint(bool mounted) {
            if (_tppPivotRotationConstraint == null) {
                return;
            }
            _tppPivotRotationConstraint.rotationAxis = mounted ? Axis.Z : Axis.Y | Axis.Z;
        }

        async UniTaskVoid PerformActionAfterBodyLoad(Action action, CancellationToken cancellationToken) {
            while (BodyData == null) {
                if (!await AsyncUtil.DelayFrame(this, cancellationToken: cancellationToken)) {
                    return;
                }
            }
            action?.Invoke();
        }

        // === Discarding
        void ReleaseBodyInstance() {
            if (Raycaster != null) {
                Destroy(Raycaster);
                Raycaster = null;
            }
            
            if (_heroBodyInstance != null) {
                Target.GetTimeDependent()?.WithoutTimeComponentsOf(_heroBodyInstance);
                Destroy(_heroBodyInstance);
            }

            if (_heroBodyHandle.IsValid()) {
                _heroBodyHandle.Release();
                _heroBodyHandle = default;
            }
        }
        
        protected override IBackgroundTask OnDiscard() {
            TimeDependent td = Target.GetTimeDependent();
            td?.WithoutUpdate(ProcessUpdate)
                .WithoutLateUpdate(ProcessLateUpdate)
                .WithoutFixedUpdate(ProcessFixedUpdate)
                .WithoutTimeScaleChanged(OnTimeScaleChanged)
                .WithoutTimeComponentsOf(gameObject);
            GetComponentsInChildren<IView>().ForEach(v => {
                if (!ReferenceEquals(v, this)) {
                    try {
                        v.Discard();
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            });
            World.Only<GameCamera>().ResetCinemachineCamera();
            ReleaseBodyInstance();
            BodyData = null;
            return base.OnDiscard();
        }

        // === Gizmos
        void DrawGroundedSphere() {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            var position = _transform.position;
            Gizmos.DrawSphere(
                new Vector3(position.x, position.y - GroundedData.groundedOffset, position.z),
                GroundedData.groundedRadius);
        }

        void DrawHeadCheckSphere() {
            if (!Controller && !TryGetComponent(out _controller)) {
                return;
            }

            GetHeadCheckParams(out var centerPosition, out var halfExtents, out _, out var bowingCheckStart, out var bowingCheckHalfExtents, out var bowingCheckDistance);
            Gizmos.color = bowingCameraHeight > 1f ? Color.yellow.WithAlpha(0.2f) : Color.yellow;
            Gizmos.DrawCube(bowingCheckStart + Vector3.up * bowingCheckDistance * 0.5f, 2 * bowingCheckHalfExtents + Vector3.up * bowingCheckDistance);
            Gizmos.color = headCollided ? Color.red.WithAlpha(0.7f) : Color.green.WithAlpha(0.7f);
            Gizmos.DrawCube(centerPosition, 2 * halfExtents);
        }

        void DrawPommelBox() {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(FirePoint.position, FirePoint.rotation, FirePoint.lossyScale);
            Gizmos.DrawCube(new Vector3(0, 0, Data.pushColliderSize.z / 2), Data.pushColliderSize);
        }

        void OnDrawGizmosSelected() {
            if (!Application.isPlaying) return;
            DrawGroundedSphere();
            DrawHeadCheckSphere();
            DrawPommelBox();
        }
    }

    internal class TimeScaleCache {
        (float value, int frame) OlderTimeScaleModifier { get; set; }
        (float value, int frame) NewerTimeScaleModifier { get; set; }

        public TimeScaleCache() {
            OlderTimeScaleModifier = (1, 0);
            NewerTimeScaleModifier = (1, 0);
        }

        public void UpdateTimeScale(float value, int frame) {
            if (NewerTimeScaleModifier.frame == frame) {
                NewerTimeScaleModifier = (value, frame);
                return;
            }
            OlderTimeScaleModifier = NewerTimeScaleModifier;
            NewerTimeScaleModifier = (value, frame);
        }

        float GetCachedTimeScaleModifierForFrame(int frame) {
            if (NewerTimeScaleModifier.frame + 1 >= frame) {
                return OlderTimeScaleModifier.value;
            }
            return NewerTimeScaleModifier.value;
        }

        public float GetTimeScaleMultiplier(int frame) {
            var cachedModifier = GetCachedTimeScaleModifierForFrame(frame);
            return cachedModifier == 0 ? 0.0f : 1.0f / cachedModifier;
        }
    }
}