using Awaken.TG.Graphics.VFX;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class HeroBodyData : MonoBehaviour {
        [FoldoutGroup("Cinemachine")] public GameObject cinemachineHeadTarget;

        [FoldoutGroup("Spatial Placing")] public Transform mainHand;
        [FoldoutGroup("Spatial Placing")] public Transform offHand;
        [FoldoutGroup("Spatial Placing")] public Transform mainHandWrist;
        [FoldoutGroup("Spatial Placing")] public Transform offHandWrist;
        [FoldoutGroup("Spatial Placing")] public Transform head;
        [FoldoutGroup("Spatial Placing")] public Transform torso;
        [FoldoutGroup("Spatial Placing")] public Transform firePoint;
        [FoldoutGroup("Spatial Placing")] public Transform aimAssistTargetFollower;
        [FoldoutGroup("Spatial Placing")] public Transform hips;
        [FoldoutGroup("Spatial Placing")] public Transform leftElbow;
        [FoldoutGroup("Spatial Placing")] public VFXBodyMarker vfxBodyMarker;
        [FoldoutGroup("Spatial Placing")] public Transform tppPivot;
        
        [FoldoutGroup("Death")] public AnimationCurve deathSlowDownCurve;
        [FoldoutGroup("Death")] public Transform mainHandParent, offHandParent, leftLeg, rightLeg, spine, spine2;
    }
}