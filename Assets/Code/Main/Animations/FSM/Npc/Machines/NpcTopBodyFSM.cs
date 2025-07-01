using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.Machines {
    public partial class NpcTopBodyFSM : NpcAnimatorSubstateMachine {
        public sealed override bool IsNotSaved => true;

        public override NpcFSMType Type => NpcFSMType.TopBodyFSM;
        public override NpcStateType DefaultState => NpcStateType.None;
        protected override bool EnableOnInitialize => true;

        // === Constructor
        public NpcTopBodyFSM(Animator animator, ARNpcAnimancer animancer, int layerIndex, AvatarMask avatarMask) : base(animator, animancer, layerIndex, avatarMask) { }

        protected override void OnInitialize() {
            AddState(new NpcNone());
            // --- Gestures
            AddState(new CustomGesticulate());
            // --- Blocking
            AddState(new BlockHold());
            // --- Deflect
            AddState(new NpcDeflectPhysical());
            AddState(new NpcDeflectMagic());
            base.OnInitialize();
        }
    }
}