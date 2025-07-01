using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.HitStops {
    public class HitStopsAsset : ScriptableObject {
        [FoldoutGroup("1H")] public HitStopData lightAttack1HData;
        [FoldoutGroup("1H")] public HitStopData heavyAttack1HData;
        [FoldoutGroup("2H")] public HitStopData lightAttack2HData;
        [FoldoutGroup("2H")] public HitStopData heavyAttack2HData;
        [Header("Used only by default settings set in HeroControllerData")]
        [FoldoutGroup("Dual")] public HitStopData heavyAttackDualHandedData;
        [FoldoutGroup("Dual")] public HitStopData forwardAttackDualHandedData;
    }
}
