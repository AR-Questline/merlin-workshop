using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    public class AnimatorResetTriggersOnEnter : StateMachineBehaviour {

        public string[] triggers = Array.Empty<string>();

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            foreach (var trigger in triggers) {
                animator.ResetTrigger(trigger);
            }
        }
    }
}
