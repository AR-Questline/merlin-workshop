using Awaken.Utility;
using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Custom {
    public sealed partial class CustomGesticulate : NpcAnimatorState<NpcAnimatorSubstateMachine> {
        public override ushort TypeForSerialization => SavedModels.CustomGesticulate;

        public override NpcStateType Type => NpcStateType.CustomGesticulate;
        public override float EntryTransitionDuration => 0.5f;

        public override void Enter(float _, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null) {
            // Gestures are played explicitly
            onNodeLoaded?.Invoke(null);
        }

        public void PlayClip(AnimationClip animationClip) {
            var fadeMode = FadeMode.FixedSpeed;
            if (CurrentState != null && CurrentState.Key == AnimancerLayer.Root.GetKey(animationClip)) {
                fadeMode = FadeMode.FromStart;
            }
            
            CurrentState = AnimancerLayer.Play(animationClip, EntryTransitionDuration, fadeMode);
            NpcAnimancer.RebindAnimationRigging();
        }
    }
}