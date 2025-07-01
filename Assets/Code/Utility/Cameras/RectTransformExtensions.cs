using System.Linq;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.Utility.Cameras {
    public static class RectTransformExtensions {
        /// <summary>
        /// Counts the bounding box corners of the given RectTransform that are visible from the given Camera in screen space.
        /// </summary>
        /// <returns>The amount of bounding box corners that are visible from the Camera.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        private static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera, float heightOffset) {
            float offset = (Screen.width / (float)Screen.height) * heightOffset;
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height - offset); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            Vector3 tempScreenSpaceCorner; // Cached
            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
            {
                tempScreenSpaceCorner = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space
                if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
                {
                    visibleCorners++;
                }
            }

            return visibleCorners;
        }

        private static int CountCornersVisible(this RectTransform rectTransform, float heightOffset) {
            float offset = (Screen.width / (float) Screen.height) * heightOffset;
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height - offset); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);
            int visibleCorners = 0;
            for (var i = 0; i < objectCorners.Length; i++) { // For each corner in rectTransform
                if (screenBounds.Contains(objectCorners[i])) { // If the corner is inside the screen
                    visibleCorners++;
                }
            }
            return visibleCorners;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsFullyVisible(this RectTransform rectTransform, float heightOffset = 0) {
            return CountCornersVisible(rectTransform, heightOffset) == 4; // True if all 4 corners are visible
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool IsVisibleHorizontalOnly(this RectTransform rectTransform) {
            float min = 0f;
            float max = Screen.width;
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);
            return objectCorners.All(c => c.x >= min && c.x <= max);
        }

        /// <summary>
        /// Determines if this RectTransform is fully visible from the specified camera.
        /// Works by checking if each bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is fully visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        [UnityEngine.Scripting.Preserve]
        public static bool IsFullyVisibleFrom(this RectTransform rectTransform, Camera camera, float heightOffset = 0) {
            return CountCornersVisibleFrom(rectTransform, camera, heightOffset) == 4; // True if all 4 corners are visible
        }

        /// <summary>
        /// Determines if this RectTransform is at least partially visible from the specified camera.
        /// Works by checking if any bounding box corner of this RectTransform is inside the cameras screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is at least partially visible from the specified camera; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        [UnityEngine.Scripting.Preserve]
        public static bool IsVisibleFrom(this RectTransform rectTransform, Camera camera) {
            return CountCornersVisibleFrom(rectTransform, camera, 0) > 0; // True if any corners are visible
        }

        public static Rect GetPixelsRect(this RectTransform rectTransform) {
            using var worldCorners = RentedArray<Vector3>.Borrow(4);
            rectTransform.GetWorldCorners(worldCorners.GetBackingArray());
            var result = new Rect(
                worldCorners[0].x,
                worldCorners[0].y,
                worldCorners[2].x - worldCorners[0].x,
                worldCorners[2].y - worldCorners[0].y);
            return result;
        }
        
        public static void StretchToParent(this RectTransform rect, Transform parent) {
            rect.SetParent(parent);
            rect.StretchToParent();
        }
        
        public static void StretchToParent(this RectTransform rect) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMax = Vector2.zero;
            rect.offsetMin = Vector2.zero;
        }
    }
}