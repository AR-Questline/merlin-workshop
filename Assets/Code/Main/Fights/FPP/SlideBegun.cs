using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class SlideBegun : StateMachineBehaviour {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            HeroMovementSystem currentMovementSystem = Hero.Current.MovementSystem;
            if (currentMovementSystem is not HumanoidMovementBase movement) {
                Log.Critical?.Error("Sliding requires DefaultMovement. Movement is: " + currentMovementSystem.Type);
                return;
            }
            movement.SlideBegun();
        }
    }
}