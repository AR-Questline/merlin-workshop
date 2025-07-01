using Awaken.TG.Code.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    public class AnimationRandomTrigger : StateMachineBehaviour {

        public string triggerName;
        public int triggerChance = 10;

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (RandomUtil.UniformInt(0, 100) < triggerChance) {
                animator.SetTrigger(triggerName);
            }
        }
    }
}