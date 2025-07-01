using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.FSM.Npc.States.Rotation;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.Machines {
    public partial class NpcOverridesFSM : NpcAnimatorSubstateMachine {
        public sealed override bool IsNotSaved => true;

        readonly bool _isAdditive;

        public override NpcFSMType Type => NpcFSMType.OverridesFSM;
        public override NpcStateType DefaultState => NpcStateType.None;
        public NpcDeath.DeathAnimType DeathAnimType { get; private set; }
        protected override bool EnableOnInitialize => true;

        // === Constructor
        public NpcOverridesFSM(Animator animator, ARNpcAnimancer animancer, int layerIndex, AvatarMask avatarMask, bool isAdditive) : base(animator, animancer, layerIndex, avatarMask) {
            _isAdditive = isAdditive;
        }

        protected override void OnInitialize() {
            NpcAnimancer.Layers[LayerIndex].IsAdditive = _isAdditive;
            AddState(new NpcNone());
            AddState(new GetHit());
            AddState(new PoiseBreakFront());
            AddState(new PoiseBreakBackRight());
            AddState(new PoiseBreakBack());
            AddState(new PoiseBreakBackLeft());
            AddState(new OverrideSpecialAttack());
            AddState(new NpcDeath());
            AddState(new CustomActionOverride());
            // --- Rotate
            AddState(new NpcRotateLeft45());
            AddState(new NpcRotateLeft90());
            AddState(new NpcRotateLeft180());
            AddState(new NpcRotateRight45());
            AddState(new NpcRotateRight90());
            AddState(new NpcRotateRight180());
            
            base.OnInitialize();
        }

        public void SetDeathAnimationType(NpcDeath.DeathAnimType newType) {
            DeathAnimType = newType;
        }

        protected override void OnDisable(bool fromDiscard) {
            if (CurrentAnimatorState is NpcDeath) {
                AnimancerLayer.SetMask(null);
                AnimancerLayer.IsAdditive = false;
                AnimancerLayer.SetWeight(1);
                return;
            }
            
            base.OnDisable(fromDiscard);
        }
    }
}