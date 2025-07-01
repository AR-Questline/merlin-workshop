using UnityEngine;

namespace Awaken.TG.Utility.Cameras {
    public static class ScreenSizeUtil {
        [UnityEngine.Scripting.Preserve]
        public static float GetPercentOfScreenOccupiedByBounds(Collider collider, Camera cam = null) {
            return GetPercentOfScreenOccupiedByBounds(collider.bounds);

        }

        public static float GetPercentOfScreenOccupiedByBounds(Bounds bounds, Camera cam = null) {
            cam ??= Camera.main;
            Rect occupiedArea = GetSizeOnScreen(bounds, cam);
            return (occupiedArea.height * occupiedArea.width)
                   / (Screen.height * Screen.width) 
                   * 100;
            
        }

        [UnityEngine.Scripting.Preserve]
        public static float GetPercentOfScreenOccupiedByBounds(Rect rect) {
            var resultantPercent = (rect.height * rect.width)
                                   / (Screen.height * Screen.width)
                                   * 100;
            return resultantPercent > 100 ? 100 : resultantPercent;
            
        }

        [UnityEngine.Scripting.Preserve]
        public static float GetPercentScreenSizeHeight(float heightOfObject) {
            var resultantPercent = heightOfObject / Screen.height * 100;
            return resultantPercent > 100 ? 100 : resultantPercent;
        }

        /// <summary>
        /// Calculates the size of <paramref name="bounds"/> on the screen of <paramref name="cam"/> as a Rect in Camera space
        /// </summary>
        /// <remarks>https://answers.unity.com/questions/49943/is-there-an-easy-way-to-get-on-screen-render-size.html</remarks>
        /// <param name="bounds">The collider to get visible size of</param>
        /// <param name="cam"></param>
        /// <returns>the Rect in camera view that <paramref name="bounds"/> occupies</returns>
        public static Rect GetSizeOnScreen(Bounds bounds, Camera cam = null) {
            Vector3 cen = bounds.center;
            Vector3 ext = bounds.extents;
            cam ??= Camera.main;
            float screenHeight = Screen.height;

            Vector2 min = cam.WorldToScreenPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z));
            Vector2 max = min;
 
 
            //0
            Vector2 point = min;
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            //1
            point = cam.WorldToScreenPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
 
            //2
            point = cam.WorldToScreenPoint(new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            //3
            point = cam.WorldToScreenPoint(new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            //4
            point = cam.WorldToScreenPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            //5
            point = cam.WorldToScreenPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            //6
            point = cam.WorldToScreenPoint(new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            //7
            point = cam.WorldToScreenPoint(new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
 
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
    }
}