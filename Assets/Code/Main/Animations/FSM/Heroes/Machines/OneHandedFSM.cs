using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Block;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class OneHandedFSM : MeleeFSM {
        const string LayerName = "1H_MainHand";
        [UnityEngine.Scripting.Preserve] const string HeadLayerName = "1H_Head";

        public sealed override bool IsNotSaved => true;
        
        SynchronizedHeroSubstateMachine _offHand, _head, _tppActiveLayer, _tppOffHandActiveLayer;

        public override string ParentLayerName => LayerName;
        public override bool UseBlockWithoutShield => !EnableAdditionalLayer;
        public override bool EnableAdditionalLayer => OffHandItem is {IsBlocking: true};
        public override bool CanBlock {
            get {
                if (!base.CanBlock) {
                    return false;
                }
                var offHandItem = OffHandItem;
                if (offHandItem == null || offHandItem.CanBeUsedAsShield) {
                    return true;
                }
                // If Hero OffHandItem is cutOffHand allow to block with MainHand
                return OffHandItem.Template == CommonReferences.Get.HandCutOffItemTemplate;
            }
        }

        public override SynchronizedHeroSubstateMachine AdditionalSynchronizedLayer => _offHand;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;
        public override HeroLayerType LayerType => HeroLayerType.MainHand;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;

        // === Constructor
        public OneHandedFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        protected override void OnInitialize() {
            base.OnInitialize();
            _offHand = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.OffHand));
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadMainHand));
            if (Hero.TppActive) {
                _tppActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveMainHand,
                    overridenAvatarMask: _tppActiveAvatarMask));
                _tppOffHandActiveLayer = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.ActiveOffHand,
                    overridenAvatarMask: _tppOffHandActiveAvatarMask, layerToSynchronize: HeroLayerType.OffHand));
            }
        }

        protected override void OnDisable(bool fromDiscard) {
            base.OnDisable(fromDiscard);
            if (!fromDiscard) {
                _tppActiveLayer?.SetEnable(false);
                _tppOffHandActiveLayer?.SetEnable(false);
            }
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