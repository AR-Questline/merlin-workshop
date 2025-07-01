using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class AttackReset : StateMachineBehaviour {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (animator.GetLayerWeight(layerIndex) < 1) {
                return;
            }
            animator.GetComponentInChildren<CharacterHandBase>()?.Owner?.Trigger(EnemyBaseClass.Events.BehaviourReset, true);
        }
    }
}
