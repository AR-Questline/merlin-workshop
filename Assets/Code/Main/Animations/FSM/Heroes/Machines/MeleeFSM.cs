using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Block;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Melee;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Animations.FSM.Heroes.States.TPP;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public abstract partial class MeleeFSM : HeroAnimatorSubstateMachine {
        public const float MaxHeavyAttackChargeDuration = 3f;
        const float BackStabVisibilityThreshold = 0.33f;
        const float ForwardAttackRequiredSprintFactor = 0.69f;

        // === Fields
        bool _canPerformLightHeldAttack;
        float _heavyAttackChargeDuration;
        
        // === Properties
        public int HeavyAttackIndex { get; set; } = -1;
        public float HeavyAttackChargePercent => _heavyAttackChargeDuration / MaxHeavyAttackChargeDuration;

        float ForwardAttackMinimumSpeed {
            get {
                float moveSpeed = ParentModel.HeroStats.MoveSpeed;
                float sprintSpeed = ParentModel.HeroStats.SprintSpeed;
                float rawAverageSpeed = math.lerp(moveSpeed, sprintSpeed, ForwardAttackRequiredSprintFactor);
                return rawAverageSpeed * ParentModel.CharacterStats.MovementSpeedMultiplier;
            }
        }
        public override bool CanBlock => Stamina.ModifiedValue > 0;
        public bool IsInHitStop => TryGetElement<MeleeHitStop>() != null;
        public bool CanPerformAction => TryGetElement<MeleeHitStop>()?.CanPerformAction ?? true;
        protected override bool CanEnableActiveLayerMask => (EnableAdditionalLayer || AnyMagicActive) && base.CanEnableActiveLayerMask;
        protected bool AnyMagicActive => ParentModel.Elements<MagicFSM>().Any(m => m.IsLayerActive);
        bool CanUseHeavyAttacks => ParentModel.Development.CanUseHeavyAttack;
        bool CanPommel => ParentModel.Development.CanPommel;
        bool ShouldUseForwardAttack => _moveInput.magnitude > 0.5f && HorizontalSpeed > ForwardAttackMinimumSpeed && ParentModel.Development.CanSprintAttack;
        protected override AvatarMask AvatarMask {
            get {
                if (!Hero.TppActive) {
                    return base.AvatarMask;
                }

                if (AnyMagicActive) {
                    return base.AvatarMask;
                }
                
                return EnableAdditionalLayer || !IsLayerActive
                    ? base.AvatarMask
                    : CommonReferences.Get.GetTppMask(HeroLayerType.BothHands);
            }
        }

        // === Constructor
        protected MeleeFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            // --- General
            InitializeGeneralStates();
            // --- Light Attacks
            InitializeLightAttackStates();
            // --- Heavy Attacks
            InitializeHeavyAttackStates();
            // --- Blocking
            InitializeBlockingStates();
            // --- BackStabbing
            InitializeBackStabStates();
        }

        protected virtual void InitializeGeneralStates() {
            AddState(new Idle());
            AddState(new MovementState());
            AddState(new EquipWeapon());
            AddState(new UnEquipWeapon());
            AddState(new EmptyState());
        }
        
        protected virtual void InitializeLightAttackStates() {
            AddState(new LightAttackInitial());
            AddState(new LightAttackTired());
            AddState(new LightAttackForward());
            AddState(new LightAttack());
        }
        
        protected virtual void InitializeHeavyAttackStates() {
            AddState(new HeavyAttackStart());
            AddState(new HeavyAttackWait());
            AddState(new HeavyAttackEnd());
        }
        
        protected virtual void InitializeBlockingStates() {
            AddState(new BlockStart());
            AddState(new BlockLoop());
            AddState(new BlockPommel());
            AddState(new BlockImpact());
            AddState(new BlockExit());
            AddState(new BlockParry());
        }

        protected virtual void InitializeBackStabStates() {
            AddState(new BackStabEnter());
            AddState(new BackStabLoop());
            AddState(new BackStabAttack());
            AddState(new BackStabExit());
        }

        // === Listener Callbacks
        protected override void OnUIStateChanged(UIState state) {
            base.OnUIStateChanged(state);
            if (state.IsMapInteractive || !state.PauseTime) {
                return;
            }

            if (CurrentAnimatorState?.GeneralType == HeroGeneralStateType.Block) {
                ParentModel.TryGetElement<HeroBlock>()?.Discard();
            }
            
            SetCurrentState(HeroStateType.Idle);
            ParentModel.FoV.EndBowZoomFoV();
        }
        
        protected override void OnHideWeapons(bool instant) {
            if (!IsLayerActive) {
                return;
            }
            if (ParentModel.Elements<MagicFSM>().Any(m => m.IsCasting)) {
                SetCurrentState(HeroStateType.Idle);
            } else {
                SetCurrentState(HeroStateType.UnEquipWeapon, instant ? 0 : null);
            }
        }

        // === Life Cycle
        protected override void OnUpdate(float deltaTime) {
            if (!_isMapInteractive) {
                CurrentAnimatorState?.Update(deltaTime);
                return;
            }

            AdditionalSynchronizedLayer?.SetEnable(EnableAdditionalLayer);

            if (CurrentAnimatorState == null) {
                return;
            }

            if (CurrentAnimatorState.GeneralType != HeroGeneralStateType.HeavyAttack) {
                _heavyAttackChargeDuration = 0;
            }

            if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.General) {
                GeneralStateUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.LightAttack) {
                LightAttackStateUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.HeavyAttack) {
                HeavyAttackStateUpdate(deltaTime);
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.Block) {
                BlockStateUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.BackStab) {
                BackStabStateUpdate();
            }
            
            CurrentAnimatorState.Update(deltaTime);
        }

        protected override void OnInputUpdate(float deltaTime) {
            if (_attackUp) {
                _canPerformLightHeldAttack = true;
            } else if (!CanUseHeavyAttacks) {
                _attackUp = _attackLongHeld && _canPerformLightHeldAttack;
            }
        }

        protected override void OnInputReset() {
            _canPerformLightHeldAttack = true;
        }
        
        // === State Updates
        void GeneralStateUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (IsBackStabAvailable) {
                SetCurrentState(HeroStateType.BackStabEnter);
                return;
            }
            
            if (WantToBlock && CanBlock) {
                SetCurrentState(CurrentAnimatorState.Type == HeroStateType.BlockExit ? HeroStateType.BlockLoop : HeroStateType.BlockStart);
                return;
            }
            
            if (WantToPerformLightAttack && Stamina.ModifiedValue > 0) {
                if (TryTriggerFinisher()) {
                    return;
                }
                
                OnLightAttackRelease();
                HeroStateType desiredAttack = ShouldUseForwardAttack ? HeroStateType.LightAttackForward : HeroStateType.LightAttackInitial;
                SetCurrentState(IsInHitStop ? HeroStateType.LightAttackFirst : desiredAttack);
                return;
            }

            if (_attackLongHeld && Stamina.ModifiedValue > 0 && CanUseHeavyAttacks) {
                if (TryTriggerFinisher()) {
                    return;
                }

                HeavyAttackIndex = -1;
                SetCurrentState(HeroStateType.HeavyAttackStart);
            }
        }

        void LightAttackStateUpdate() {
            if (WantToBlock && CanBlock) {
                SetCurrentState(HeroStateType.BlockStart);
                return;
            }
            
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            TryPerformNextAttack();
        }

        void HeavyAttackStateUpdate(float deltaTime) {
            if (WantToBlock && CanBlock) {
                SetCurrentState(HeroStateType.BlockStart);
                return;
            }
            
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (CurrentAnimatorState.Type == HeroStateType.HeavyAttackEnd) {
                TryPerformNextAttack();
                return;
            }
            
            _heavyAttackChargeDuration += deltaTime;
            if (Stamina.ModifiedValue <= 0) {
                SetCurrentState(HeroStateType.HeavyAttackEnd);
                return;
            }

            if (!_attackLongHeld) {
                SetCurrentState(HeroStateType.HeavyAttackEnd);
            }
        }

        void TryPerformNextAttack() {
            if (WantToPerformLightAttack && Stamina.ModifiedValue > 0) {
                if (TryTriggerFinisher()) {
                    return;
                }
                
                OnLightAttackRelease();
                if (GeneralStateType == HeroGeneralStateType.HeavyAttack) {
                    SetCurrentState(HeroStateType.LightAttackInitial);
                    return;
                }
                if (ShouldUseForwardAttack) {
                    SetCurrentState(HeroStateType.LightAttackForward);
                    return;
                }
                SetCurrentState(HeroStateType.LightAttackFirst);
                return;
            }
                
            if (_attackLongHeld && Stamina.ModifiedValue > 0 && CanUseHeavyAttacks) {
                if (TryTriggerFinisher()) {
                    return;
                }
                    
                SetCurrentState(HeroStateType.HeavyAttackStart);
            }
        }

        void BlockStateUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (!CanBlock) {
                SetCurrentState(HeroStateType.BlockExit);
                return;
            }
            if (WantToPerformLightAttack && Stamina.ModifiedValue > 0 && CanPommel) {
                SetCurrentState(HeroStateType.BlockPommel);
            } 
        }

        void BackStabStateUpdate() {
            if (WantToBlock && CanBlock) {
                SetCurrentState(HeroStateType.BlockStart);
                return;
            }
            
            if (!CurrentAnimatorState.CanPerformNewAction) return;
            
            if (!IsBackStabAvailable) {
                SetCurrentState(HeroStateType.BackStabExit);
                return;
            }

            if ((WantToPerformLightAttack || _attackLongHeld) && Stamina.ModifiedValue > 0) {
                if (TryTriggerFinisher()) {
                    return;
                }
                OnLightAttackRelease();
                SetCurrentState(HeroStateType.BackStabAttack);
            }
        }

        protected override void OnEnteredState(HeroAnimatorState state) {
            if (state.GeneralType != HeroGeneralStateType.General) {
                CancelHitStop();
            }
            if (state.GeneralType != HeroGeneralStateType.Block) {
                ParentModel.RemoveElementsOfType<HeroBlock>();
            }
        }

        bool TryTriggerFinisher() {
            return ParentModel.FinisherHandling.TryTriggerFinisherBeforeAttack();
        }

        // === Input Helpers
        void OnLightAttackRelease() {
            _canPerformLightHeldAttack = false;
        }

        bool WantToPerformLightAttack => _attackUp || (IsInAttackProlong && !_attackHeld && !_attackDown);
        bool WantToBlock => IsInBlockProlong || BlockHeld;

        public virtual bool IsBackStabAvailable => ParentModel.IsCrouching
                                                   && ParentModel.HeroCombat.MaxHeroVisibility <= 0.99f
                                                   && DistanceSqrToBackStabAble < ParentModel.Data.BackStabRangeSqr
                                                   && !ParentModel.HasElement<PreventBackStab>();
        float DistanceSqrToBackStabAble {
            get {
                if (ParentModel.VHeroController.Raycaster.NPCRef.TryGet(out Location location) &&
                    location.TryGetElement(out NpcElement npc) && npc.NpcAI.HeroVisibility <= BackStabVisibilityThreshold) {
                    return (npc.Coords - ParentModel.Coords).sqrMagnitude;
                }
                return float.MaxValue;
            }
        }

        // === Toggling Enable
        protected override void AfterEnable() {
            AdditionalSynchronizedLayer?.SetEnable(EnableAdditionalLayer, 1);
            HeadLayerIndex?.SetEnable(_cameraShakesEnabled, CameraShakesIntensity);
        }

        protected override void OnDisable(bool fromDiscard) {
            CancelHitStop(fromDiscard);
            AdditionalSynchronizedLayer?.SetEnable(false);
            HeadLayerIndex?.SetEnable(false);
        }

        protected override void AttachListeners() {
            base.AttachListeners();
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageBlocked, () => SetCurrentState(HeroStateType.BlockImpact), this);
        }
        
        // === HitStop
        public void HitStop(HitStopData hitStopData) {
            if (ParentModel.IsInHitStop) {
                return;
            }
            AddElement(new MeleeHitStop(hitStopData));
        }

        void CancelHitStop(bool instant = false) {
            TryGetElement<MeleeHitStop>()?.ExitHitStop(instant);
        }
    }
}