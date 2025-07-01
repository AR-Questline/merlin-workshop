using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.Machines {
    public partial class NpcAdditiveFSM : NpcAnimatorSubstateMachine {
        const string LayerName = "Additive";

        public sealed override bool IsNotSaved => true;

        public override NpcFSMType Type => NpcFSMType.AdditiveFSM;
        public override string ParentLayerName => LayerName;
        public override NpcStateType DefaultState => NpcStateType.None;
        protected override bool EnableOnInitialize => true;

        // === Constructor
        public NpcAdditiveFSM(Animator animator, ARNpcAnimancer animancer, int layerIndex, AvatarMask avatarMask) : base(animator, animancer, layerIndex, avatarMask) { }
        
        protected override void OnInitialize() {
            NpcAnimancer.Layers[LayerIndex].IsAdditive = true;
            AddState(new NpcNone());
            AddState(new GetHit());
            base.OnInitialize();
        }
    }
}