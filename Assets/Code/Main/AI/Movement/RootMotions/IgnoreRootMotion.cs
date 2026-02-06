using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.RootMotions {
    public class IgnoreRootMotion : MonoBehaviour {
        void OnAnimatorMove() {
            // This function existing stops all root motion from being applied by animator.
            // See: https://docs.unity3d.com/ScriptReference/Animator-applyRootMotion.html
        }
    }
}
