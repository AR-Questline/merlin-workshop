using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Fights.Utils {
    public class AnimatorRandomCycleOffset : StateMachineBehaviour {

        readonly int _id = Animator.StringToHash("CycleOffset");
        bool _randomValueSet;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) {
            if (!_randomValueSet) {
                animator.SetFloat(_id, Random.value);
                _randomValueSet = true;
            }
        }
    }
}
