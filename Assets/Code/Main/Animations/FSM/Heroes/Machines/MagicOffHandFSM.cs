using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Animations.FSM.Heroes.States.TPP;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class MagicOffHandFSM : MagicFSM {
        const string LayerName = "Magic_OffHand";

        public sealed override bool IsNotSaved => true;

        SynchronizedHeroSubstateMachine _head, _tppActiveLayer;
        public override string ParentLayerName => LayerName;
        public override CastingHand CastingHand => CastingHand.OffHand;
        public override HeroLayerType LayerType => HeroLayerType.OffHand;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        // === Constructor
        public MagicOffHandFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadOffHand, isAdditive: true));
            if (Hero.TppActive) {
                _tppActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveOffHand,
                    overridenAvatarMask: _tppOffHandActiveAvatarMask));
            }
            // --- General
            AddState(new Idle());
            AddState(new MovementState());
            AddState(new EquipWeapon());
            AddState(new UnEquipWeapon());
            AddState(new EmptyState());
            AddState(new MagicFailedCast());
            // --- Magic Casting
            AddState(new MagicLightInitial());
            AddState(new MagicLightSequence());
            
            AddState(new MagicHeavyStart());
            AddState(new MagicHeavyChargeLoop());
            AddState(new MagicHeavyChargeIncrease());
            AddState(new MagicHeavyLoop());
            AddState(new MagicHeavyEnd());
            AddState(new MagicCancelCast());
        }

        // === Life Cycle
        protected override void OnMagicFSMUpdate(float deltaTime) {
            if (!_isMapInteractive) {
                if (CurrentAnimatorState != null) {
                    CurrentStateUpdate(deltaTime);
                }
                return;
            }

            if (CurrentAnimatorState == null) {
                return;
            }

            if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.General) {
                GeneralStateUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.MagicCastLight) {
                LightCastUpdate();
            } else if (CurrentAnimatorState.GeneralType == HeroGeneralStateType.MagicCastHeavy) {
                HeavyCastUpdate();
            }

            CurrentStateUpdate(deltaTime);
        }

        void CurrentStateUpdate(float deltaTime) {
            CurrentAnimatorState.Update(deltaTime);
            if (CurrentAnimatorState is ISynchronizedAnimatorState) {
                Synchronize(ActiveTopBodyLayer()?.CurrentAnimatorState, CurrentAnimatorState, deltaTime);
            }
        }

        // === State Updates
        void GeneralStateUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;
            
            if (WantToPerformLightCast) {
                TryEnterMagicCastState(HeroStateType.MagicLightInitial, true);
                return;
            }
            
            if (BlockLongHeld) {
                TryEnterMagicCastState(HeroStateType.MagicHeavyStart, false);
            }
        }

        void LightCastUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;
            
            if (WantToPerformLightCast) {
                TryEnterMagicCastState(HeroStateType.MagicLightFirst, true);
                return;
            }
            
            if (BlockLongHeld) {
                TryEnterMagicCastState(HeroStateType.MagicHeavyStart, false);
            }
        }
        
        void HeavyCastUpdate() {
            if (!CurrentAnimatorState.CanPerformNewAction) return;

            if (!BlockLongHeld) {
                SetCurrentState(HeroStateType.MagicHeavyEnd, MagicEndBlendDuration);
            }
        }

        public override void ResetProlong() {
            ResetBlockProlong();
        }

        protected override void OnDisable(bool fromDiscard) {
            base.OnDisable(fromDiscard);
            if (!fromDiscard) {
                _tppActiveLayer?.SetEnable(false);
            }
        }

        protected override void DetermineTppLayerMask() {
            bool activeMaskCondition = CanEnableActiveLayerMask && CurrentAnimatorState.UsesActiveLayerMask;
            _tppActiveLayer.SetEnable(activeMaskCondition, activeMaskCondition ? 1 : 0, ActiveMaskBlendSpeed);
        }
        
        // === Input Helpers
        bool WantToPerformLightCast => BlockUp || (IsInBlockProlong && !BlockHeld && !BlockDown);
    }
}