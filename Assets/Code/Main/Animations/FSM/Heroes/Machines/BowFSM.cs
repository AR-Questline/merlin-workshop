using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Bow;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class BowFSM : HeroAnimatorSubstateMachine {
        [UnityEngine.Scripting.Preserve] const string LayerName = "2H_Bow";
        [UnityEngine.Scripting.Preserve] const string HeadLayerName = "2H_Bow_Head";
        [UnityEngine.Scripting.Preserve] const string FmodShootingForce = "ShootingForce";
        const float MaxArrowVelocity = 60;

        public sealed override bool IsNotSaved => true;
        
        // === Fields & Properties
        SynchronizedHeroSubstateMachine _head;
        CharacterBow _bow;
        StatTweak _slowSpeedModifier, _aimSensitivityMultiplierModifier;
        ProjectilePreload _defaultPreload;
        ProjectilePreload _customPreload;
        ItemProjectile _projectile;
        bool _wasInBowDrawState;
        bool _isSlowedDown;
        
        public override string ParentLayerName => LayerName;
        public override bool UseAlternateState => !HasArrows;
        public bool WasCanceledWhenInBowHold { get; private set; }
        public bool PullingRangedWeapon => IsLayerActive && CurrentAnimatorState?.GeneralType == HeroGeneralStateType.BowDraw;
        public float FireStrength { get; private set; }
        public override bool PreventHidingWeapon => CurrentAnimatorState?.GeneralType == HeroGeneralStateType.BowDraw;
        public override HeroLayerType LayerType => HeroLayerType.BothHands;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        public CharacterBow HeroBow {
            get {
                if (_bow is { HasBeenDiscarded: false }) {
                    return _bow;
                }
                
                return _bow = StatsItem?.View<CharacterBow>();
            }
        }

        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;
        Stat SpeedMultiplier => ParentModel.CharacterStats.MovementSpeedMultiplier;
        Stat AimSensitivityMultiplier => ParentModel.HeroStats.AimSensitivityMultiplier;
        HeroControllerData Data => ParentModel.Data;
        Transform FirePoint => ParentModel.VHeroController.FirePoint;
        Item EquippedArrow => ParentModel.Inventory.EquippedItem(EquipmentSlotType.Quiver);
        bool HasArrows => EquippedArrow != null;
        
        // === Events
        public new static class Events {
            public static readonly Event<Hero, bool> OnBowRelease = new(nameof(OnBowRelease));
        }
        
        // === Constructor
        public BowFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }
        
        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadBothHands));
            // --- General
            AddState(new BowIdle());
            AddState(new MovementState());
            AddState(new BowEquipWeapon());
            AddState(new BowUnEquipWeapon());
            AddState(new EmptyState());
            AddState(new BowCancelDraw());
            // --- Bow Attacks
            AddState(new BowPull());
            AddState(new BowHold());
            AddState(new BowRelease());
            
            _defaultPreload = ItemProjectile.PreloadDefaultProjectile();

            World.EventSystem.ListenTo(EventSelector.AnySource, HeroLogicModifiers.Events.DisableBowPenaltiesToggled, this, OnBowPenaltiesToggled);
        }

        // === Listeners Callbacks
        void OnQuiverChanged(ICharacterInventory _) {
            VHeroController vHeroController = ParentModel.VHeroController;
            if (vHeroController == null || vHeroController.PerspectiveChangeInProgress) {
                return;
            }
            
            UpdateArrowProjectile();
            SetCurrentState(HeroStateType.EquipWeapon);
        }

        protected override void OnUIStateChanged(UIState state) {
            base.OnUIStateChanged(state);
            if (state.IsMapInteractive || !state.PauseTime) {
                return;
            }

            if (CurrentAnimatorState?.GeneralType == HeroGeneralStateType.BowDraw) {
                CancelBowDraw();
            } else {
                SetCurrentState(HeroStateType.Idle);
            }
        }

        // === Life Cycle
        protected override void OnUpdate(float deltaTime) {
            if (!_isMapInteractive) {
                CurrentAnimatorState?.Update(deltaTime);
                return;
            }

            if (CurrentAnimatorState == null) {
                return;
            }

            if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.General) {
                GeneralStateUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.BowDraw) {
                BowDrawStateUpdate();
            }

            CurrentAnimatorState.Update(deltaTime);
            if (CurrentAnimatorState == null) {
                return;
            }
            UpdateBowPullState();

            if (PullingRangedWeapon && Stamina.Percentage <= 0.9f) {
                float exponent = ParentModel.ProficiencyStats.Archery.ModifiedValue.Remap(10, 100, 0.15f, 0.01f, true);
                float shakeValue = (-1 * Mathf.Pow(Stamina.Percentage, exponent)) + 1;
                shakeValue *= ParentModel.HeroStats.BowSwayMultiplier.ModifiedValue;
                SetBowCameraShakeState(true, shakeValue);
            }
        }

        void UpdateBowPullState() {
            bool isPullingBow = CurrentAnimatorState.Type is HeroStateType.BowPull or HeroStateType.BowHold;
            if (!_wasInBowDrawState && isPullingBow) {
                _wasInBowDrawState = true;
            } else if (_wasInBowDrawState && !isPullingBow) {
                EndBowDrawState();
            }
        }

        void GeneralStateUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if ((_attackHeld || _attackLongHeld) && HasArrows && StaminaUsedUpEffect.CanDecreaseContinuously()) {
                SetCurrentState(HeroStateType.BowPull);
            }
        }

        void BowDrawStateUpdate() {
            if (CurrentAnimatorState.Type == HeroStateType.BowHold) {
                FireStrength = 1;
            } else {
                FireStrength = Mathf.Max(0.1f, CurrentAnimatorState.TimeElapsedNormalized);
            }

            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (!_attackLongHeld && !_attackHeld) {
                SetCurrentState(HeroStateType.BowRelease);
            }
        }

        void CancelBowDraw(float crossFadeDuration = 0.25f) {
            WasCanceledWhenInBowHold = CurrentAnimatorState?.Type == HeroStateType.BowHold;
            SetCurrentState(HeroStateType.BowCancelDraw, crossFadeDuration);
            ParentModel.FoV.EndBowZoomFoV();
            FireStrength = 0;
        }

        // === Public API
        public void BeginSlowModifier() {
            _isSlowedDown = true;
            ApplySlowSpeedModifier();
            
            if (_aimSensitivityMultiplierModifier == null || _aimSensitivityMultiplierModifier.HasBeenDiscarded) {
                _aimSensitivityMultiplierModifier = StatTweak.Multi(AimSensitivityMultiplier, Data.aimSensitivityMultiplier, TweakPriority.Multiply, this);
            }
        }

        public void EndSlowModifier() {
            _isSlowedDown = false;
            
            _slowSpeedModifier?.Discard();
            _slowSpeedModifier = null;
            
            _aimSensitivityMultiplierModifier?.Discard();
            _aimSensitivityMultiplierModifier = null;
        }

        void OnBowPenaltiesToggled(bool penaltiesDisabled) {
            if (penaltiesDisabled && _slowSpeedModifier is { HasBeenDiscarded: false }) {
                _slowSpeedModifier.Discard();
                _slowSpeedModifier = null;
            } else if (!penaltiesDisabled && _isSlowedDown) {
                ApplySlowSpeedModifier();
            }
        }

        void ApplySlowSpeedModifier() {
            if (Hero.Current.LogicModifiers.DisableBowPullMovementPenalties) {
                return;
            }
            
            if (_slowSpeedModifier is { HasBeenDiscarded: false }) {
                return;
            }
            
            _slowSpeedModifier = StatTweak.Multi(SpeedMultiplier, Data.bowDrawnSpeedMultiplier, TweakPriority.Multiply, this);
        }

        public void FireProjectile() {
            Transform visualFirePoint = ParentModel.MainHandWeapon.VisualFirePoint ?? FirePoint;
            Vector3 velocity = CalculateArrowVelocity(visualFirePoint.position, MaxArrowVelocity * FireStrength, out var offsetData);
            InstantiateProjectile(visualFirePoint, velocity, offsetData).Forget();
        }

        async UniTaskVoid InstantiateProjectile(Transform visualFirePoint, Vector3 velocity, ProjectileOffsetData? offsetData) {
            CombinedProjectile result;
            if (_projectile != null) {
                result = await _projectile.GetProjectile(true, null, visualFirePoint, null);
            } else {
                result = await ItemProjectile.GetDefaultArrow(true, null, visualFirePoint, null);
            }
            if (result.logic != null) {
                FireProjectileInternal(result.logic, velocity, FireStrength, visualFirePoint, offsetData);
            }
        }

        void EndBowDrawState() {
            ParentModel.Trigger(ICharacter.Events.OnBowDrawEnd, ParentModel);
            _wasInBowDrawState = false;
        }

        // === Toggling Enable
        protected override void AfterEnable() {
            UpdateArrowProjectile();
            HeadLayerIndex?.SetEnable(_cameraShakesEnabled, CameraShakesIntensity);
        }

        protected override void OnDisable(bool fromDiscard) {
            EndSlowModifier();
            ParentModel.FoV.EndBowZoomFoV();
            SetBowCameraShakeState(false, 0);
            EndBowDrawState();
        }

        protected override void AttachListeners() {
            base.AttachListeners();
            ParentModel.Inventory.ListenTo(ICharacterInventory.Events.SlotChanged(EquipmentSlotType.Quiver), OnQuiverChanged, this);
        }

        protected override void OnHideWeapons(bool instant) {
            if (!IsLayerActive) {
                return;
            }

            if (PreventHidingWeapon) {
                CancelBowDraw(0);
            } else {
                SetCurrentState(HeroStateType.UnEquipWeapon, instant ? 0 : null);
            }
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            ReleaseArrowProjectile();
            base.OnDiscard(fromDomainDrop);
        }

        // === Helpers
        void FireProjectileInternal(GameObject projectileInstance, Vector3 arrowVelocity, float fireStrength, Transform firePoint, ProjectileOffsetData? offsetData) {
            SetBowCameraShakeState(false, 0);
            if (projectileInstance == null) {
                return;
            }

            CharacterBow bow = (CharacterBow)ParentModel.MainHandWeapon;
            
            // --- Audio
            bow.PlayAudioClip(ItemAudioType.ReleaseBow, false, new FMODParameter(FmodShootingForce, fireStrength));
            
            // --- SettingUp params
            Item equippedArrows = ParentModel.HeroItems.EquippedItem(EquipmentSlotType.Quiver);

            DamageDealingProjectile projectile = projectileInstance.GetComponent<DamageDealingProjectile>();
            projectile.SetVelocityAndForward(arrowVelocity, offsetData);
            projectile.owner = ParentModel;
            projectile.SetBaseDamageParams(MainHandItem, equippedArrows, fireStrength);
            if (projectile is Arrow a && equippedArrows != null) {
                a.SetItemTemplate(equippedArrows.Template);
            }
            projectile.FinalizeConfiguration();

            // --- Reducing projectile from inventory
            using var suspendNotification = new AdvancedNotificationBuffer.SuspendNotifications<ItemNotificationBuffer>();
            equippedArrows?.DecrementQuantity();

            FireStrength = 0;
        }
        
        public static Vector3 CalculateArrowVelocity(Vector3 firePoint, float magnitude, out ProjectileOffsetData? offsetData) {
            var mainCameraTransform = World.Only<CameraStateStack>().MainCamera.transform;
            var mainCameraForward = mainCameraTransform.forward;
            var mainCameraFirePoint = mainCameraTransform.position + mainCameraForward;
            if (firePoint != mainCameraFirePoint) {
                offsetData = ProjectileOffsetData.BowOffsetParams(firePoint, mainCameraFirePoint);
            } else {
                offsetData = null;
            }
            return mainCameraForward * magnitude;
        }
        
        void SetBowCameraShakeState(bool enable, float shakeMagnitude) {
            if (enable) {
                var inputFromCode = ParentModel.TryGetElement<ForcedInputFromCode>() ??
                                    ParentModel.AddElement<ForcedInputFromCode>();
                inputFromCode.UpdateInputMagnitude(shakeMagnitude);
            } else {
                ParentModel.TryGetElement<ForcedInputFromCode>()?.Discard();
            }
        }

        void UpdateArrowProjectile() {
            Item quiverItem = ParentModel.HeroItems.EquippedItem(EquipmentSlotType.Quiver);
            if (quiverItem != null && quiverItem.TryGetElement(out ItemProjectile itemProjectile)) {
                if (_projectile != itemProjectile) {
                    _customPreload.Release();
                    _projectile = itemProjectile;
                    _customPreload = _projectile.PreloadProjectile();
                }
            } else {
                if (_projectile != null) {
                    _customPreload.Release();
                    _projectile = null;
                }
            }
        }

        void ReleaseArrowProjectile() {
            _defaultPreload.Release();
            _customPreload.Release();
        }
    }
}