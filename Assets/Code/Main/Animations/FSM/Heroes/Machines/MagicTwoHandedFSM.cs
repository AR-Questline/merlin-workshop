using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class MagicTwoHandedFSM : MagicMainHandFSM {
        [UnityEngine.Scripting.Preserve] const string LayerName = "Magic_2H";
        [UnityEngine.Scripting.Preserve] const string HeadLayerName = "Magic_2H_Head";
        [UnityEngine.Scripting.Preserve] const string BlockStateName = "Block";

        SynchronizedHeroSubstateMachine _head;
        public override Item StatsItem => MainHandItem;
        public override string ParentLayerName => LayerName;
        public override CastingHand CastingHand => CastingHand.BothHands;
        public override HeroLayerType LayerType => HeroLayerType.BothHands;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        public override float LightAttackCost => StatsItemStats?.LightAttackCost.ModifiedValue ?? 0;
        public override float HeavyAttackCost => StatsItemStats?.HeavyAttackCost.ModifiedValue ?? 0;
        public override float PushCost => StatsItemStats?.PushStaminaCost.ModifiedValue ?? 0;
        public override bool CanBlock => Stamina.ModifiedValue > 0;
        public override bool EnableAdditionalLayer => false;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        public MagicTwoHandedFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadBothHands));
        }
        
        protected override void DetermineTppLayerMask() { }
    }
}