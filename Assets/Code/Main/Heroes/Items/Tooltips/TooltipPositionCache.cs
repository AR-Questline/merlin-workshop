using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public readonly struct TooltipPositionCache {
        public readonly Vector3 anchorPosition;
        public readonly Vector2 alignmentPivot;

        public TooltipPositionCache(Vector3 anchorPosition, Vector2 alignmentPivot) {
            this.anchorPosition = anchorPosition;
            this.alignmentPivot = alignmentPivot;
        }
    }
}