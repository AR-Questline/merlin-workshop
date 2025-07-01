using UnityEngine;

namespace Awaken.TG.Main.Heroes.Animations {
    public class AnimationStateBehaviour : StateMachineBehaviour {

        public AnimationEvent animationEvent;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            animator.GetComponentInParent<IAnimationStateListener>()?.OnStateEnter(animationEvent);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            animator.GetComponentInParent<IAnimationStateListener>()?.OnStateExit(animationEvent);
        }
    }
}
