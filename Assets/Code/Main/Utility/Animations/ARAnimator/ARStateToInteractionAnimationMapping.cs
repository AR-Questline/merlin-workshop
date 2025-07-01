using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [CreateAssetMenu(menuName = "TG/Animancer/StateToInteractionAnimationMapping", order = 0)]
    public class ARStateToInteractionAnimationMapping : ARStateToAnimationMapping {
        [SerializeField] public InteractionAnimationData interactionData;
    }

    [Serializable]
    public struct InteractionAnimationData {
        public CustomEquipWeaponType customEquipWeapon;
        public float blendDuration;
        public bool rotateToCombatTarget;

        public static InteractionAnimationData Default() => new InteractionAnimationData() {
            customEquipWeapon = CustomEquipWeaponType.Default,
            blendDuration = 0f,
            rotateToCombatTarget = false,
        };
    }
    
    public enum CustomEquipWeaponType : byte {
        Default,
        Custom,
        Sitting,
        Crouching,
        Lying,
    }
}