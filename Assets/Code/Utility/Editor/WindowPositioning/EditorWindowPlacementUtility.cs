using UnityEngine;

namespace Awaken.Utility.Editor.WindowPositioning {
    public static class EditorWindowPlacementUtility {
        /// <summary>
        /// Calculates a subwindow position adjacent to a source window, staying within the same monitor.
        /// </summary>
        public static Vector2 CalculatePositionForSubWindow(Rect sourceWindowRect, Rect desiredWindowRect, float padding = 8) {
            Vector2 pos = desiredWindowRect.position;
            
            // Get monitor bounds where the source window is located
#if UNITY_EDITOR_WIN
            var monitorBounds = User32Utils.GetMonitorBoundsForPoint(new Vector2Int((int)sourceWindowRect.center.x, (int)sourceWindowRect.center.y));
#else
            var monitorBounds = new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
#endif
            // Try to place it to the right of the source window
            float subWindowRightEdge = sourceWindowRect.xMax + desiredWindowRect.size.x + padding;
            bool fitsOnRight = subWindowRightEdge <= monitorBounds.xMax;

            if (fitsOnRight) {
                pos.x = sourceWindowRect.xMax + padding;
            } else {
                // Place to the left
                pos.x = sourceWindowRect.xMin - desiredWindowRect.size.x - padding;
            }

            // Adjust Y if needed
            float bottomEdge = pos.y + desiredWindowRect.size.y;
            if (bottomEdge > monitorBounds.yMax) {
                pos.y = monitorBounds.yMax - desiredWindowRect.size.y;
            }

            return pos;
        }
        
        public static Vector2 GetTrueMousePosition() {
#if UNITY_EDITOR_WIN
            return User32Utils.GetMousePosition();
#else
            return GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
#endif
        }
        
    }
}
