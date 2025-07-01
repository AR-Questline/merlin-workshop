using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Melee;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class DualHandedFSM : MeleeFSM {
        const string MainHandLayerName = "Dual_MainHand";
        [UnityEngine.Scripting.Preserve] const string OffHandLayerName = "Dual_OffHand";
        [UnityEngine.Scripting.Preserve] const string HeadLayerName = "Dual_Head";

        public sealed override bool IsNotSaved => true;

        SynchronizedHeroSubstateMachine _offHand, _head, _tppActiveLayer, _tppOffHandActiveLayer;

        public override Item StatsItem => IsUsingMainHand ? MainHandItem : OffHandItem;
        public override string ParentLayerName => MainHandLayerName;
        public override HeroLayerType LayerType => HeroLayerType.DualMainHand;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        public bool IsInDualHandedAttack => CurrentAnimatorState is HeavyAttackEnd or LightAttackForward && !OffHandItem.IsFists;
        public bool IsUsingMainHand {
            get {
                if (CurrentAnimatorState is MeleeAttackAnimatorState attackState) {
                    return attackState.IsUsingMainHand;
                }
                return true;
            }
        }
        public WeaponRestriction Restriction {
            get {
                if (IsInDualHandedAttack) {
                    return WeaponRestriction.None;
                }
                return IsUsingMainHand ? WeaponRestriction.MainHand : WeaponRestriction.OffHand;
            }
        }

        public override bool EnableAdditionalLayer => true;
        public override SynchronizedHeroSubstateMachine AdditionalSynchronizedLayer => _offHand;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        // === Events
        public new static class Events {
            public static readonly Event<Hero, Hero> DualWieldingStarted = new(nameof(DualWieldingStarted));
            public static readonly Event<Hero, Hero> DualWieldingEnded = new(nameof(DualWieldingEnded));
        }

        // === Constructor
        public DualHandedFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }
        
        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            _offHand = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.DualOffHand, false));
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadBothHands));
            if (Hero.TppActive) {
                _tppActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveMainHand,
                    overridenAvatarMask: _tppActiveAvatarMask));
                _tppOffHandActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveOffHand,
                    overridenAvatarMask: _tppOffHandActiveAvatarMask, layerToSynchronize: HeroLayerType.DualOffHand));
            }
        }

        protected override void InitializeLightAttackStates() {
            AddState(new LightAttackInitial());
            AddState(new LightAttackTired());
            AddState(new DualHandedLightAttackForward());
            AddState(new LightAttack());
        }
        
        protected override void InitializeHeavyAttackStates() {
            AddState(new HeavyAttackStart());
            AddState(new HeavyAttackWait());
            AddState(new DualHandedHeavyAttackEnd());
        }
        
        protected override void InitializeBackStabStates() {
            AddState(new BackStabEnter());
            AddState(new BackStabLoop());
            AddState(new DualWieldingBackStabAttack());
            AddState(new BackStabExit());
        }
        
        // === Lifecycle
        protected override void OnEnable() {
            base.OnEnable();
            ParentModel.Trigger(Events.DualWieldingStarted, ParentModel);
        }

        protected override void OnDisable(bool fromDiscard) {
            base.OnDisable(fromDiscard);
            if (!fromDiscard) {
                _tppActiveLayer?.SetEnable(false);
                _tppOffHandActiveLayer?.SetEnable(false);
            }
            ParentModel.Trigger(Events.DualWieldingEnded, ParentModel);
        }

        protected override void DetermineTppLayerMask() {
            bool activeMaskCondition = CanEnableActiveLayerMask && CurrentAnimatorState.UsesActiveLayerMask;
            bool mainActive = activeMaskCondition && IsUsingMainHand;
            bool offActive = activeMaskCondition && !IsUsingMainHand;
            
            _tppActiveLayer.SetEnable(mainActive, mainActive ? 1 : 0, ActiveMaskBlendSpeed);
            _tppOffHandActiveLayer.SetEnable(offActive, offActive ? 1 : 0, ActiveMaskBlendSpeed);
        }
    }
}