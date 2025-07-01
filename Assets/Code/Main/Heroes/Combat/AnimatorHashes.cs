using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public static class AnimatorHashes {
        // === General
        [UnityEngine.Scripting.Preserve] public static readonly int Reset = Animator.StringToHash("Reset");
        
        // === Combat
        [UnityEngine.Scripting.Preserve] public static readonly int LightAttack = Animator.StringToHash("LightAttack");
        [UnityEngine.Scripting.Preserve] public static readonly int ToggleWeapon = Animator.StringToHash("WeaponEquipped");
        [UnityEngine.Scripting.Preserve] public static readonly int StateTime = Animator.StringToHash("StateTime");
        [UnityEngine.Scripting.Preserve] public static readonly int Hit = Animator.StringToHash("Hit");
        [UnityEngine.Scripting.Preserve] public static readonly int Finish = Animator.StringToHash("Finish");
        
        // === Locomotion
        [UnityEngine.Scripting.Preserve] public static readonly int Idle = Animator.StringToHash("Idle");
        [UnityEngine.Scripting.Preserve] public static readonly int Walking = Animator.StringToHash("Walking");
        [UnityEngine.Scripting.Preserve] public static readonly int Velocity = Animator.StringToHash("Velocity");
        [UnityEngine.Scripting.Preserve] public static readonly int Grounded = Animator.StringToHash("Grounded");
        [UnityEngine.Scripting.Preserve] public static readonly int Jump = Animator.StringToHash("Jump");
        [UnityEngine.Scripting.Preserve] public static readonly int FreeFall = Animator.StringToHash("FreeFall");
        
        // === Actions
        [UnityEngine.Scripting.Preserve] static readonly int BurnWyrdcandle = Animator.StringToHash("BurnWyrdcandle");

        // === Pets
        [UnityEngine.Scripting.Preserve]
        public static readonly int Pet = Animator.StringToHash("Pet");
    }
}