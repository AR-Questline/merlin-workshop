using UnityEngine;

namespace Awaken.Utility.Extensions {
    public static class RectUtils {
        public static Rect Inflated(in this Rect rect, float x, float y) {
            return new Rect(rect.x - x, rect.y - y, rect.width + x * 2, rect.height + y * 2);
        }
        
        public static Vector2 PosWithOffset(in this Rect rect, Vector2 offset = default) {
            return new Vector2(rect.position.x + offset.x, rect.position.y + offset.y);
        }
        
        /// <summary>
        /// Check full size overlap of other rect in world space with half size offset
        /// </summary>
        public static bool OverlapInWorldSpace(in this Rect current, Rect other) {
            var currentMax = PosWithOffset(current, current.size * 0.5f);
            var currentMin = PosWithOffset(current, current.size * (0.5f * -1));
            var otherMax = PosWithOffset(other, other.size * 0.5f);
            var otherMin = PosWithOffset(other, other.size * (0.5f * -1));
            
            return otherMax.x > currentMin.x
                   && otherMin.x < currentMax.x
                   && otherMax.y > currentMin.y
                   && otherMin.y < currentMax.y;
        }
        
        /// <summary>
        /// Check full size overlap of point in world space with half size offset
        /// </summary>
        public static bool ContainsInWorldSpace(in this Rect current, Vector2 point) {
            var currentMax = PosWithOffset(current, current.size * 0.5f);
            var currentMin = PosWithOffset(current, current.size * (0.5f * -1));
            
            return point.x > currentMin.x
                   && point.x < currentMax.x
                   && point.y > currentMin.y
                   && point.y < currentMax.y;
        }
    }
}