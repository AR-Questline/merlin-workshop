using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class BowIdle : StateMachineBehaviour {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (animator.GetLayerWeight(layerIndex) < 1) {
                return;
            }
            animator.GetComponentsInChildren<CharacterBow>().ForEach(b => b.OnBowIdle());
        }
    }
}