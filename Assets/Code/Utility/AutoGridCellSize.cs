using System;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Utility {
    public class AutoGridCellSize : MonoBehaviour {
        public GridLayoutGroup grid;
        public RectTransform rectTransform;
        
        [Range(0f, 1f)]
        public float upPadding = 0;
        [Range(0f, 1f)]
        public float rightPadding = 0;
        [Range(0f, 1f)]
        public float bottomPadding = 0;
        [Range(0f, 1f)]
        public float leftPadding = 0;
        [Range(0f, 1f)]
        public float spacingX = 0;
        [Range(0f, 1f)]
        public float spacingY = 0;

        [ContextMenu("SetUp")]
        void Update() {
            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;

            if (width > 0.1f && height > 0.1f) {
                var columns = grid.constraintCount;

                grid.padding = new RectOffset((int)(width * leftPadding), (int)(width * rightPadding), (int)(height * upPadding), (int)(height * bottomPadding));
                grid.spacing = new Vector2(width * spacingX, height * spacingY);

                var cellWidth = (width * (1f - (rightPadding + leftPadding + (spacingX * (columns -1))))) / columns;
                grid.cellSize = new Vector2(cellWidth, cellWidth);

                if (Application.isPlaying) {
                    Destroy(this);
                }
            }
        }
    }
}