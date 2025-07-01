using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.Machines {
    public partial class NpcCustomActionsFSM : NpcAnimatorSubstateMachine {
        public sealed override bool IsNotSaved => true;

        public bool StoryLoop { get; set; }
        public bool StoryLoopTalking { get; set; }
        
        public override NpcFSMType Type => NpcFSMType.CustomActionsFSM;
        protected override bool EnableOnInitialize => true;
        public override NpcStateType DefaultState => NpcStateType.None;
        
        // === Events
        public new static class Events {
            public static readonly Event<NpcElement, bool> CustomStateExited = new(nameof(CustomStateExited));
        }
        
        // === Constructor
        public NpcCustomActionsFSM(Animator animator, ARNpcAnimancer animancer, int layerIndex, AvatarMask avatarMask) : base(animator, animancer, layerIndex, avatarMask) { }

        protected override void OnInitialize() {
            AddState(new NpcNone());
            AddState(new CustomEnter());
            AddState(new CustomLoop());
            AddState(new CustomStoryLoop());
            AddState(new CustomStoryLoopTalking());
            AddState(new CustomExit());
            AddState(new CustomGesticulate());
            
            base.OnInitialize();
        }
    }
}