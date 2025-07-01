using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class AnimatorDrakeAnimatedPropertiesControllerTrigger : StateMachineBehaviour {
        [SerializeField] bool reverse;
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (animator.GetLayerWeight(layerIndex) < 1) {
                return;
            }

            foreach (var controller in animator.GetComponentsInChildren<DrakeAnimatedPropertiesOverrideController>()) {
                if (reverse) {
                    controller.StartBackward();
                } else {
                    controller.StartForward();
                }
            }
        }
    }
}