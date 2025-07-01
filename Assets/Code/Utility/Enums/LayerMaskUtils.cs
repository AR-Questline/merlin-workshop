using UnityEngine;

namespace Awaken.Utility.Enums {
    public static class LayerMaskUtils {
        public static bool Contains(this LayerMask mask, int layer) {
            return (mask & (1 << layer)) != 0;
        }
    }
}