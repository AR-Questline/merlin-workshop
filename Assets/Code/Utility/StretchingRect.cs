using Awaken.Utility.Maths.Data;
using Unity.Mathematics;

namespace Awaken.Utility {
    /// <summary>
    /// When updated, moves to contain fixed rect, and stretches internal rect to contain as much previously contained area as possible.  
    /// </summary>
    public struct StretchingRect {
        public MinMaxAABR value;

        public StretchingRect(MinMaxAABR initialRect) {
            this.value = initialRect;
        }

        public void Update(MinMaxAABR fixedRect, float maxStretchDistance) {
            MoveLeftOrBottom(ref value.min.x, fixedRect.min.x, maxStretchDistance);
            MoveLeftOrBottom(ref value.min.y, fixedRect.min.y, maxStretchDistance);

            MoveRightOrTop(ref value.max.x, fixedRect.max.x, maxStretchDistance);
            MoveRightOrTop(ref value.max.y, fixedRect.max.y, maxStretchDistance);
        }

        static void MoveLeftOrBottom(ref float expandableRectCornerPos, float fixedRectCornerPos, float maxStretchDistance) {
            var dist = fixedRectCornerPos - expandableRectCornerPos;
            var newDist = math.clamp(dist, 0, maxStretchDistance);
            expandableRectCornerPos = fixedRectCornerPos - newDist;
        }
        
        static void MoveRightOrTop(ref float expandableRectCornerPos, float fixedRectCornerPos, float maxStretchDistance) {
            var dist = expandableRectCornerPos - fixedRectCornerPos;
            var newDist = math.clamp(dist, 0, maxStretchDistance);
            expandableRectCornerPos = fixedRectCornerPos + newDist;
        }
    }
}