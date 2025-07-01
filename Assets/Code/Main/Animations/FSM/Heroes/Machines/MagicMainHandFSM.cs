using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Block;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class MagicMainHandFSM : MagicFSM {
        const string LayerName = "Magic_MainHand";

        public sealed override bool IsNotSaved => true;
        
        SynchronizedHeroSubstateMachine _offHand, _head, _tppActiveLayer, _tppOffHandActiveLayer;
        bool _canPommel = true, _waitForAttackUpEnd;
        
        public override Item StatsItem => MainHandItem;
        public override string ParentLayerName => LayerName;
        public override CastingHand CastingHand => CastingHand.MainHand;
        public override HeroLayerType LayerType => HeroLayerType.MainHand;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        // --- When equipping magic in main hand we can only attack & push with OffHand item, that's why we use OffHandItemStats
        public override float LightAttackCost => OffHandItemStats?.LightAttackCost.ModifiedValue ?? 0;
        public override float HeavyAttackCost => OffHandItemStats?.HeavyAttackCost.ModifiedValue ?? 0;
        public override float PushCost => OffHandItemStats?.PushStaminaCost.ModifiedValue ?? 0;
        public override bool CanBlock => EnableAdditionalLayer && Stamina.ModifiedValue > 0;
        public override bool EnableAdditionalLayer => OffHandItem is { CanBeUsedAsShield: true };
        public override SynchronizedHeroSubstateMachine AdditionalSynchronizedLayer => _offHand;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;
        protected override AvatarMask AvatarMask {
            get {
                if (!Hero.TppActive) {
                    return base.AvatarMask;
                }

                if (ParentModel.TryGetElement<MagicOffHandFSM>()?.IsLayerActive ?? false) {
                    return base.AvatarMask;
                }
                
                return EnableAdditionalLayer || !IsLayerActive
                    ? base.AvatarMask
                    : CommonReferences.Get.GetTppMask(HeroLayerType.BothHands);
            }
        }
        // === Constructor
        public MagicMainHandFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }
        
        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            _offHand = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.OffHand));
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadMainHand, isAdditive: true));
            if (Hero.TppActive) {
                _tppActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveMainHand,
                    overridenAvatarMask: _tppActiveAvatarMask));
                _tppOffHandActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveOffHand,
                    overridenAvatarMask: _tppOffHandActiveAvatarMask, layerToSynchronize: HeroLayerType.OffHand));
            }
            // --- General
            AddState(new Idle());
            AddState(new MovementState());
            AddState(new EquipWeapon());
            AddState(new UnEquipWeapon());
            AddState(new EmptyState());
            AddState(new MagicFailedCast());
            // --- Light
            AddState(new MagicLightInitial());
            AddState(new MagicLightSequence());
            // --- Heavy
            AddState(new MagicHeavyStart());
            AddState(new MagicHeavyChargeLoop());
            AddState(new MagicHeavyChargeIncrease());
            AddState(new MagicHeavyLoop());
            AddState(new MagicHeavyEnd());
            // --- Utility
            AddState(new MagicCancelCast());
            // --- Blocking
            AddState(new BlockStart());
            AddState(new BlockLoop());
            AddState(new BlockPommel());
            AddState(new BlockImpact());
            AddState(new BlockExit());
            AddState(new BlockParry());
        }

        protected override void OnEnable() {
            base.OnEnable();
            AdditionalSynchronizedLayer?.SetEnable(EnableAdditionalLayer, 1);
        }

        // === Life Cycle
        protected override void OnMagicFSMUpdate(float deltaTime) {
            if (!_isMapInteractive) {
                CurrentAnimatorState?.Update(deltaTime);
                return;
            }

            AdditionalSynchronizedLayer?.SetEnable(EnableAdditionalLayer);

            if (CurrentAnimatorState == null) {
                return;
            }

            VerifyCanPommel();

            if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.General) {
                GeneralStateUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.MagicCastLight) {
                LightCastUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.MagicCastHeavy) {
                HeavyCastUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.Block) {
                BlockStateUpdate();
            }
            
            CurrentAnimatorState.Update(deltaTime);
        }
        
        // === State Updates
        void GeneralStateUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (WantToBlock && CanBlock) {
                SetCurrentState(HeroStateType.BlockStart);
                return;
            }

            if (WantToPerformLightAttack) {
                TryEnterMagicCastState(HeroStateType.MagicLightInitial, true);
                return;
            }
            
            if (_attackLongHeld) {
                TryEnterMagicCastState(HeroStateType.MagicHeavyStart, false);
            }
        }

        void LightCastUpdate() {
            if (WantToBlock && CanBlock) {
                EnterBlockFromCast();
                return;
            }
            
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (WantToPerformLightAttack) {
                TryEnterMagicCastState(HeroStateType.MagicLightFirst, true);
                return;
            }
            
            if (_attackLongHeld) {
                TryEnterMagicCastState(HeroStateType.MagicHeavyStart, false);
            }
        }
        
        void HeavyCastUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;
            
            if (WantToBlock && CanBlock) {
                EnterBlockFromCast();
                return;
            }

            if (!_attackHeld) {
                SetCurrentState(HeroStateType.MagicHeavyEnd, MagicEndBlendDuration);
            }
        }

        void BlockStateUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (!CanBlock) {
                SetCurrentState(HeroStateType.BlockExit);
                return;
            }
            if (WantToPerformLightAttack && Stamina.ModifiedValue >= PushCost && _canPommel) {
                SetCurrentState(HeroStateType.BlockPommel);
            }
        }
        
        void EnterBlockFromCast() {
            CancelCasting();
            SetCurrentState(HeroStateType.BlockStart);
            EndSlowModifier();
        }

        public override void CancelCasting() {
            base.CancelCasting();
            if (_attackLongHeld) {
                _canPommel = false;
            }
        }
        
        protected override void OnEnteredState(HeroAnimatorState state) {
            if (EnableAdditionalLayer && state.GeneralType != HeroGeneralStateType.Block) {
                ParentModel.RemoveElementsOfType<HeroBlock>();
            }
        }
        
        // === Input Helpers
        bool WantToPerformLightAttack => _attackUp || (IsInAttackProlong && !_attackHeld && !_attackDown);
        bool WantToBlock => BlockDown || BlockHeld;
        
        // === Helpers
        void VerifyCanPommel() {
            if (_attackUp && !_canPommel && !_waitForAttackUpEnd) {
                _waitForAttackUpEnd = true;
            } else if (!_attackUp && _waitForAttackUpEnd) {
                _canPommel = true;
                _waitForAttackUpEnd = false;
            }
        }
        
        // === Toggling Enable
        protected override void AfterEnable() {
            AdditionalSynchronizedLayer?.SetEnable(EnableAdditionalLayer, 1);
            _canPommel = true;
        }

        protected override void OnDisable(bool fromDiscard) {
            AdditionalSynchronizedLayer?.SetEnable(false);
            base.OnDisable(fromDiscard);
            if (!fromDiscard) {
                _tppActiveLayer?.SetEnable(false);
                _tppOffHandActiveLayer?.SetEnable(false);
            }
        }

        protected override void AttachListeners() {
            base.AttachListeners();
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageBlocked, () => SetCurrentState(HeroStateType.BlockImpact), this);
        }
        
        protected override void DetermineTppLayerMask() {
            bool activeMaskCondition = CanEnableActiveLayerMask && CurrentAnimatorState.UsesActiveLayerMask;
            bool isBlockState = CurrentAnimatorState is BlockStateBase;
            bool mainActive = activeMaskCondition && !isBlockState;
            bool offActive = activeMaskCondition && isBlockState;
            _tppActiveLayer.SetEnable(mainActive, mainActive ? 1 : 0, ActiveMaskBlendSpeed);
            _tppOffHandActiveLayer.SetEnable(offActive, offActive ? 1 : 0, ActiveMaskBlendSpeed);
        }
    }
}