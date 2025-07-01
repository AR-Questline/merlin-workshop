using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public partial class TwoHandedFSM : MeleeFSM {
        const string LayerName = "2H";

        public sealed override bool IsNotSaved => true;

        SynchronizedHeroSubstateMachine _head;
        
        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.BothHands;
        public override HeroStateType DefaultState => HeroStateType.EquipWeapon;
        protected override SynchronizedHeroSubstateMachine HeadLayerIndex => _head;

        // === Constructor
        public TwoHandedFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }

        protected override void OnInitialize() {
            base.OnInitialize();
            _head = AddElement(new SynchronizedHeroSubstateMachine(HeroLayerType.HeadBothHands));
        }
    }
}