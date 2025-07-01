using System;
using System.Collections.Generic;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Animations.FSM.Shared;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    /// <summary>
    /// Each AnimatorSubstateMachine corresponds to one (sometimes two) layer in animator.
    /// </summary>
    public abstract partial class HeroAnimatorSubstateMachine : ARAnimatorSubstateMachine<Hero> {
        protected const float ActiveMaskBlendSpeed = 5f;
        const float AttackProlongDuration = 0.5f;
        
        // === Fields & Properties
        protected AvatarMask _avatarMask;
        protected AvatarMask _tppActiveAvatarMask;
        protected AvatarMask _tppOffHandActiveAvatarMask;
        protected bool _isMapInteractive = true;
        protected PlayerInput _input;
        readonly Dictionary<HeroStateType, HeroAnimatorState> _states = new();
        
        HeroStaminaUsedUpEffect _staminaUsedUpEffect;
        
        public ARHeroAnimancer HeroAnimancer { get; }
        public virtual bool UseBlockWithoutShield => false;
        public virtual bool UseAlternateState => false;
        public virtual bool CanBlock => Stamina.ModifiedValue > 0;
        public virtual bool PreventHidingWeapon => false;
        public abstract HeroLayerType LayerType { get; }
        public HeroGeneralStateType GeneralStateType => CurrentAnimatorState?.GeneralType ?? HeroGeneralStateType.General;
        public HeroStateType CurrentStateType => CurrentAnimatorState?.Type ?? HeroStateType.Empty;
        public HeroStateType CurrentStateToEnterType => CurrentAnimatorState?.StateToEnter ?? HeroStateType.Empty;
        public HeroAnimatorState CurrentAnimatorState { get; private set; }
        public abstract HeroStateType DefaultState { get; }
        protected override int LayerIndex => (int)LayerType;
        protected bool IsInsideSafeZone => ParentModel.HasElement<PacifistMarker>();
        protected virtual bool CanBeUpdatedInSafeZone => false;
        protected virtual SynchronizedHeroSubstateMachine HeadLayerIndex => null;
        protected override AvatarMask AvatarMask => _avatarMask;
        protected virtual bool CanEnableActiveLayerMask => _tppActiveAvatarMask != null && _tppOffHandActiveAvatarMask != null;
        public virtual bool EnableAdditionalLayer => false;
        public virtual SynchronizedHeroSubstateMachine AdditionalSynchronizedLayer => null;

        // === Stats & Costs
        public LimitedStat Stamina => ParentModel.Stamina;
        public HeroStaminaUsedUpEffect StaminaUsedUpEffect => ParentModel.CachedElement(ref _staminaUsedUpEffect);
        [UnityEngine.Scripting.Preserve] public LimitedStat Mana => ParentModel.Mana;
        public virtual Item StatsItem => MainHandItem ?? OffHandItem;
        public ItemStats StatsItemStats => StatsItem?.ItemStats;
        public Item MainHandItem => ParentModel.MainHandItem;
        public ItemStats MainHandItemStats => MainHandItem?.ItemStats;
        public Item OffHandItem => ParentModel.OffHandItem;
        public ItemStats OffHandItemStats => OffHandItem?.ItemStats;
        [UnityEngine.Scripting.Preserve] public Stat AttackSpeedStat => StatsItemStats.ParentModel.IsTwoHanded 
            ? ParentModel.CharacterStats.TwoHandedLightAttackSpeed 
            : ParentModel.CharacterStats.OneHandedLightAttackSpeed;
        public virtual float LightAttackCost => StatsItemStats ? StatsItemStats.LightAttackCost.ModifiedValue * ParentModel.HeroStats.ItemStaminaCostMultiplier : 0;
        public virtual float HeavyAttackCost => StatsItemStats ? StatsItemStats.HeavyAttackCost.ModifiedValue * ParentModel.HeroStats.ItemStaminaCostMultiplier : 0;
        public virtual float PushCost => StatsItemStats ? StatsItemStats.PushStaminaCost.ModifiedValue * ParentModel.HeroStats.ItemStaminaCostMultiplier : 0;
        public float HorizontalSpeed => ParentModel.HorizontalSpeed;
        public virtual float CameraShakesMultiplier { get; set; } = 1;
        
        // === Input
        public bool BlockUp { get; private set; }
        public bool BlockDown { get; private set; }
        public bool BlockHeld { get; private set; }
        public bool BlockLongHeld { get; private set; }
        
        protected bool IsInAttackProlong => Time.time < _timeWhenUsedLightAttack + AttackProlongDuration;
        protected bool IsInBlockProlong => Time.time < _timeWhenUsedBlock + AttackProlongDuration;
        protected float CameraShakesIntensity => _screenShakesSettings.Intensity * CameraShakesMultiplier;
        protected bool _attackUp, _attackDown, _attackHeld, _attackLongHeld;
        protected int _isBeginningAttack;
        protected int _isBeginningBlock;
        protected Vector2 _moveInput;
        protected bool _cameraShakesEnabled = true;
        float _timeWhenUsedLightAttack = float.MinValue;
        float _timeWhenUsedBlock = float.MinValue;
        [UnityEngine.Scripting.Preserve] float _timeWhenParryCooldownApplied = float.MinValue;
        ScreenShakesProactiveSetting _screenShakesSettings;
        protected ReversedHandsSetting _handsSettings;
        
        // === Constructor
        protected HeroAnimatorSubstateMachine(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) {
            HeroAnimancer = animancer;
        }
        
        // === Initializing
        protected override void OnInitialize() {
            // --- AvatarMask must be set before base.OnInitialize()
            if (Hero.TppActive) {
                _avatarMask = CommonReferences.Get.GetTppMask(LayerType);
                _tppActiveAvatarMask = CommonReferences.Get.GetTppActiveMask(HeroLayerType.MainHand);
                _tppOffHandActiveAvatarMask = CommonReferences.Get.GetTppActiveMask(HeroLayerType.OffHand);
            } else {
                _avatarMask = CommonReferences.Get.GetMask(LayerType);
                _tppActiveAvatarMask = null;
                _tppOffHandActiveAvatarMask = null;
            }
            base.OnInitialize();
#if UNITY_EDITOR
            AnimancerLayer.SetDebugName(LayerType.ToString());
#endif
            AnimancerLayer.SetWeight(0);
            
            _input = World.Only<PlayerInput>();
            InitializeScreenShakes();
            
            HeadLayerIndex?.AfterFullyInitialized(() => {
                HeadLayerIndex.SetEnable(_cameraShakesEnabled, CameraShakesIntensity);
            }, this);
            
            _handsSettings = World.Only<ReversedHandsSetting>();
        }
        
        void InitializeScreenShakes() {
            _screenShakesSettings = World.Only<ScreenShakesProactiveSetting>();
            ScreenShakesToggled(_screenShakesSettings);
        }

        // === Public API
        public override void EnableFSM() {
            if (IsLayerActive) {
                return;
            }
            
            BaseEnableFSM(Update);
            AnimancerLayer.SetMask(AvatarMask);
            OnEnable();
            SetCurrentState(DefaultState, 0f);
            _isMapInteractive = UIStateStack.Instance.State.IsMapInteractive;
            AttachListeners();
            AfterEnable();
        }

        public override void DisableFSM(bool fromDiscard = false) {
            if (!fromDiscard && (!CanBeDisabled || Animancer == null)) {
                return;
            }

            BaseDisableFSM(Update, fromDiscard);
            OnDisable(fromDiscard);
            
            if (!fromDiscard && this is not LegsFSM) {
                ParentModel.TryGetElement<LegsFSM>()?.UpdateAvatarMask();
            }
            
            HeadLayerIndex?.SetEnable(false);
            AdditionalSynchronizedLayer?.SetEnable(false);
            
            CurrentAnimatorState?.Exit();
            CurrentAnimatorState = null;
            AnimancerLayer.DestroyStates();
        }
        
        public bool IsCurrentState(HeroStateType stateType) {
            return CurrentAnimatorState?.Type == stateType;
        }

        public HeroAnimatorState TryGetStateOfType(HeroStateType stateType) {
            return _states.GetValueOrDefault(stateType);
        }

        public virtual void SetCurrentState(HeroStateType stateType, float? overrideCrossFadeTime = null, Action<ITransition> onNodeLoaded = null) {
            bool reEnter = false;
            if (CurrentAnimatorState != null && CurrentAnimatorState.Type == stateType) {
                if (!CurrentAnimatorState.CanReEnter) {
                    return;
                }
                reEnter = true;
            }

            float previousStateNormalizedTime = 0;
            var previousAnimatorState = CurrentAnimatorState;
            if (CurrentAnimatorState != null) {
                previousStateNormalizedTime = CurrentAnimatorState.TimeElapsedNormalized;
                CurrentAnimatorState.Exit(reEnter);
                if (CurrentAnimatorState != previousAnimatorState) {
                    // Exiting Previous Animator State triggered events that changed current state,
                    // so we want to abort here since we are no longer active state.
                    return;
                }
            }
            if (_states.TryGetValue(stateType, out var state)) {
                CurrentAnimatorState = state;
                CurrentAnimatorState?.Enter(previousStateNormalizedTime, overrideCrossFadeTime, onNodeLoaded);
                if (CurrentAnimatorState == null) {
                    return;
                }
                foreach (var fsm in Elements<SynchronizedHeroSubstateMachine>()) {
                    fsm.SetCurrentState(CurrentAnimatorState.StateToEnter, overrideCrossFadeTime);
                }
                
                if (Hero.TppActive) {
                    DetermineTppLayerMask();
                }

                OnEnteredState(CurrentAnimatorState);
                return;
            }

            CurrentAnimatorState = null;
            Log.Important?.Error($"Failed to enter state: {stateType}! No such state exists for FSM: {this}");
        }
        
        public void ApplyParryCooldown() {
            _timeWhenParryCooldownApplied = Time.time;
        }

        // === States Management
        protected void AddState(HeroAnimatorState state) {
            _states.Add(state.Type, state);
            AddElement(state);
        }

        protected virtual void OnEnteredState(HeroAnimatorState state) {}

        // === Life cycle
        protected virtual void AttachListeners() {
            ParentModel.ListenTo(Hero.Events.ShowWeapons, OnShowWeapons, this);
            ParentModel.ListenTo(Hero.Events.HideWeapons, OnHideWeapons, this);
            UIStateStack.Instance.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            World.Only<ScreenShakesProactiveSetting>().ListenTo(Setting.Events.SettingRefresh, ScreenShakesToggled, this);
        }
        
        protected virtual void OnShowWeapons(bool instant) {
            SetCurrentState(HeroStateType.EquipWeapon, instant ? 0 : null);
        }

        protected virtual void OnHideWeapons(bool instant) {
            if (IsLayerActive && !PreventHidingWeapon) {
                SetCurrentState(HeroStateType.UnEquipWeapon, instant ? 0 : null);
            }
        }

        protected virtual void OnUIStateChanged(UIState state) {
            _isMapInteractive = state.IsMapInteractive;
        }

        protected void ScreenShakesToggled(Setting setting) {
            ScreenShakesProactiveSetting screenShakesSetting = (ScreenShakesProactiveSetting) setting;
            _cameraShakesEnabled = screenShakesSetting.Enabled;
        }
        
        void Update(float deltaTime) {
            UpdateLayerWeight();
            
            if (ParentModel.IsInToolAnimation) {
                CurrentAnimatorState?.Update(deltaTime);
                return;
            }
            
            UpdateInput(deltaTime);
            
            if (IsInsideSafeZone && !CanBeUpdatedInSafeZone) {
                return;
            }
            
            OnUpdate(deltaTime);
        }

        protected virtual void UpdateLayerWeight() {
            HeadLayerIndex?.SetEnable(_cameraShakesEnabled, CameraShakesIntensity);
        }
        
        protected virtual void OnUpdate(float deltaTime) {
            CurrentAnimatorState?.Update(deltaTime);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            try {
                DisableFSM(true);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
        
        // === Input Handling
        void UpdateInput(float deltaTime) {
            _moveInput = _input.MoveInput;
            _attackUp = IsAttackUp();
            _attackDown = IsAttackDown();
            BlockUp = IsBlockUp();
            BlockDown = IsBlockDown();
            BlockHeld = IsBlockHeld();
            BlockLongHeld = IsBlockLongHeld();
            _attackHeld = IsAttackHeld();
            _attackLongHeld = IsAttackLongHeld();

            if (_attackDown && _isBeginningAttack <= 0) {
                _timeWhenUsedLightAttack = Time.time;
            } else if (_isBeginningAttack > 0) {
                _isBeginningAttack--;
            }
            
            if (BlockDown && _isBeginningBlock <= 0) {
                _timeWhenUsedBlock = Time.time;
            } else if (_isBeginningBlock > 0) {
                _isBeginningBlock--;
            }

            OnInputUpdate(deltaTime);
        }

        public void ResetInput() {
            _attackUp = false;
            _attackDown = false;
            BlockHeld = false;
            _attackHeld = false;
            _attackLongHeld = false;
            OnInputReset();
            ResetAttackProlong();
        }

        protected virtual void OnInputUpdate(float deltaTime) {}
        protected virtual void OnInputReset() {}
        
        // === Helpers
        public bool EquipInputPressed => IsAttackDown() || IsBlockHeld();
        protected virtual bool IsAttackDown() => _input.GetButtonDown(KeyBindings.Gameplay.Attack) ||
                                                 _input.GetButtonDown(KeyBindings.Gameplay.AttackHeavy) ||
                                                 _input.GetMouseDown(_handsSettings.RightHandMouseButton);
        protected virtual bool IsAttackHeld() => _input.GetButtonHeld(KeyBindings.Gameplay.Attack) ||
                                                 _input.GetButtonHeld(KeyBindings.Gameplay.AttackHeavy) ||
                                                 _input.GetMouseHeld(_handsSettings.RightHandMouseButton);
        protected virtual bool IsAttackLongHeld() => _input.GetButtonLongHeld(KeyBindings.Gameplay.AttackHeavy) ||
                                                     _input.GetMouseLongHeld(_handsSettings.RightHandMouseButton);
        protected virtual bool IsAttackUp() => _input.GetButtonUp(KeyBindings.Gameplay.Attack) ||
                                               _input.GetMouseUp(_handsSettings.RightHandMouseButton);

        protected virtual bool IsBlockUp() => _input.GetButtonUp(KeyBindings.Gameplay.Block) ||
                                            _input.GetMouseUp(_handsSettings.LeftHandMouseButton);
        protected virtual bool IsBlockDown() => _input.GetButtonDown(KeyBindings.Gameplay.Block) ||
                                                _input.GetMouseDown(_handsSettings.LeftHandMouseButton);
        protected virtual bool IsBlockHeld() => _input.GetButtonHeld(KeyBindings.Gameplay.Block) ||
                                                _input.GetMouseHeld(_handsSettings.LeftHandMouseButton);
        protected virtual bool IsBlockLongHeld() => _input.GetButtonLongHeld(KeyBindings.Gameplay.Block) ||
                                                    _input.GetMouseLongHeld(_handsSettings.LeftHandMouseButton);


        public void ResetBothProlongs() {
            ResetAttackProlong();
            ResetBlockProlong();
        }
        
        public void ResetAttackProlong() {
            _timeWhenUsedLightAttack = float.MinValue;
            _isBeginningAttack = PlayerInput.FramesProlongedInput + 1;
        }
        
        public void ResetBlockProlong() {
            _timeWhenUsedBlock = float.MinValue;
            _isBeginningBlock = PlayerInput.FramesProlongedInput + 1;
        }

        public virtual float SynchronizedStateOffsetNormalizedTime() {
            if (!Hero.TppActive) {
                return 0;
            }

            AnimancerState currentState = ParentModel.Element<LegsFSM>().CurrentAnimatorState?.CurrentState;
            if (currentState == null) {
                return 0;
            }
            return AnimancerUtils.SynchronizeNormalizedTime(currentState, ParentModel.GetDeltaTime());
        }
        
        protected virtual void DetermineTppLayerMask() { }
        
        // === AttackSpeed
        public float GetAttackSpeed(bool isHeavy) {
            return this switch {
                DualHandedFSM => isHeavy ? HeroAnimancer.heavyAttackMult1H : HeroAnimancer.lightAttackMult1H,
                OneHandedFSM => isHeavy ? HeroAnimancer.heavyAttackMult1H : HeroAnimancer.lightAttackMult1H,
                MagicMeleeOffHandFSM => isHeavy ? HeroAnimancer.heavyAttackMult1H : HeroAnimancer.lightAttackMult1H,
                TwoHandedFSM => isHeavy ? HeroAnimancer.heavyAttackMult2H : HeroAnimancer.lightAttackMult2H,
                BowFSM => HeroAnimancer.bowDrawSpeed,
                _ => 1
            };
        }
        
        // === Helpers
        protected AnimancerState GetTopBodyAnimancerState() {
            foreach (var layer in Animancer.Layers) {
                if (layer.Weight > 0) {
                    return layer.CurrentState;
                }
            }
            return null;
        }
        
        protected HeroAnimatorSubstateMachine ActiveTopBodyLayer() {
            var topBodyState = GetTopBodyAnimancerState();
            if (topBodyState == null) {
                return null;
            }
            
            return Hero.Current.Elements<HeroAnimatorSubstateMachine>()
                .FirstOrDefault(m => m.CurrentAnimatorState?.CurrentState == topBodyState);
        }
        protected void Synchronize(HeroAnimatorState parentModelState, HeroAnimatorState currentState, float deltaTime) {
            if (parentModelState == null || parentModelState.Type != currentState.Type) {
                return;
            }

            if (parentModelState.ParentModel == this) {
                return;
            }
            
            var parentState = parentModelState.CurrentState;
            currentState.CurrentState.SetNormalizedTimeWithEventsInvoke(AnimancerUtils.SynchronizeNormalizedTime(parentState, deltaTime));
        }
    }
}
