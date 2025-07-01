using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    public interface IDeathAnimationProvider {
        public bool CanPlayAnimationAfterLoad { get; }

        public static bool ShouldPlayAnimationAfterLoad(Transform t) {
            foreach (var provider in t.GetComponentsInChildren<IDeathAnimationProvider>(true)) {
                if (!provider.CanPlayAnimationAfterLoad) {
                    return false;
                }
            }
            return true;
        }
    }
}