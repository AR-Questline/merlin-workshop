using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Machines {
    public class IdleFSM : HeroAnimatorSubstateMachine {
        const string LayerName = "Idle";
        public sealed override bool IsNotSaved => true;

        public override string ParentLayerName => LayerName;
        public override HeroLayerType LayerType => HeroLayerType.Idle;
        public override HeroStateType DefaultState => HeroStateType.None;
        protected override bool CanBeDisabled => false;

        bool _weaponsVisible;
        
        // === Constructor
        public IdleFSM(Animator animator, ARHeroAnimancer animancer) : base(animator, animancer) { }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            
            AddState(new HeroNoneState());
            AddState(new HeadBobbingIdle());
            AddState(new MovementState());
            
            EnableFSM();
        }

        protected override void AttachListeners() {
            base.AttachListeners();
            Hero.Current.HeroItems.ListenTo(HeroLoadout.Events.LoadoutChanged, OnLoadoutChanged, this);
        }

        protected override void UpdateLayerWeight() {
            if (_weaponsVisible) {
                AnimancerLayer.Weight = 0;
                AnimancerLayer.Stop();
                AnimancerLayer.DestroyStates();
            } else {
                AnimancerLayer.Weight = TryGetStateOfType(CurrentStateType)?.HeadLayerWeightOverride ?? 0;
            }
            base.UpdateLayerWeight();
        }

        protected override void OnShowWeapons(bool instant) {
            if (IsInsideSafeZone) {
                return;
            }
            _weaponsVisible = true;
            UpdateLayerWeight();
            SetCurrentState(HeroStateType.None, 0f);
        }

        void OnLoadoutChanged() {
            if(_weaponsVisible) return;
            OnShowWeapons(true);
        }

        protected override void OnHideWeapons(bool instant) {
            _weaponsVisible = false;
            UpdateLayerWeight();
            SetCurrentState(HeroStateType.Idle, 0);
        }
    }
}