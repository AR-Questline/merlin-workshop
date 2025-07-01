using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.TPP;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class MagicMeleeOffHandFSM : MeleeFSM {
        public const string LayerName = "Magic_MeleeOffHand";
        [UnityEngine.Scripting.Preserve] const string HeadLayerName = "Magic_MeleeOffHand_Head";

        public sealed override bool IsNotSaved => true;

        SynchronizedHeroSubstateMachine _head, _tppActiveLayer;
        public override Item StatsItem => OffHandItem;
        public override string ParentLayerName => LayerName;
        public override bool CanBlock => false;
        public override HeroLayerType LayerType => HeroLayerType.OffHand;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;
        public override bool IsBackStabAvailable => false;
        protected override AvatarMask AvatarMask => _avatarMask;
        protected override bool CanEnableActiveLayerMask => _tppOffHandActiveAvatarMask != null;

        // === Constructor
        public MagicMeleeOffHandFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadOffHand));
            if (Hero.TppActive) {
                _tppActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveOffHand,
                    overridenAvatarMask: _tppOffHandActiveAvatarMask));
            }
        }

        protected override void InitializeBlockingStates() {
            // --- Melee weapon in offHand can't block
        }
        
        protected override bool IsAttackDown() => _input.GetButtonDown(KeyBindings.Gameplay.Block) ||
                                                  _input.GetMouseDown(_handsSettings.LeftHandMouseButton);
        protected override bool IsAttackHeld() => _input.GetButtonHeld(KeyBindings.Gameplay.Block) ||
                                                  _input.GetMouseHeld(_handsSettings.LeftHandMouseButton);
        protected override bool IsAttackLongHeld() => _input.GetButtonLongHeld(KeyBindings.Gameplay.Block) ||
                                                      _input.GetMouseLongHeld(_handsSettings.LeftHandMouseButton);
        protected override bool IsAttackUp() => _input.GetButtonUp(KeyBindings.Gameplay.Block) ||
                                                _input.GetMouseUp(_handsSettings.LeftHandMouseButton);

        protected override bool IsBlockUp() => false;
        protected override bool IsBlockDown() => false;
        protected override bool IsBlockHeld() => false;
        protected override bool IsBlockLongHeld() => false;

        protected override void OnUpdate(float deltaTime) {
            base.OnUpdate(deltaTime);
            
            if (CurrentAnimatorState is ISynchronizedAnimatorState) {
                Synchronize(ActiveTopBodyLayer()?.CurrentAnimatorState, CurrentAnimatorState, deltaTime);
            }
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
    }
}