using System;
using Awaken.TG.Code.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    public class AnimationRandomTriggersList : StateMachineBehaviour {
        public string[] triggerNames = Array.Empty<string>();

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            int index = RandomUtil.UniformInt(0, triggerNames.Length - 1);
            animator.SetTrigger(triggerNames[index]);
        }
    }
}