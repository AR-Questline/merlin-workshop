using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Fights.FPP {
    public class AnimatorRNG : StateMachineBehaviour {
        const float RepeatTime = 0.2f;

        readonly int _id = Animator.StringToHash("RandomNumber");
        float _lastRepeatTime;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) {
            _lastRepeatTime -= RepeatTime;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (stateInfo.normalizedTime >= _lastRepeatTime + RepeatTime) {
                animator.SetFloat(_id, Random.value);
                _lastRepeatTime = stateInfo.normalizedTime;
            }
        }
    }
}