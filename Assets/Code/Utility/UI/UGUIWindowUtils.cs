using UnityEngine;

namespace Awaken.Utility.UI {
    public static class UGUIWindowUtils {
        public static Rect StandardWindowPosition(WindowPositioning position) {
            var width = Screen.width * position.widthPercentage;
            var height = Screen.height * position.heightPercentage;

            if (position.position == WindowPosition.TopLeft) {
                var posX = 20;
                var posY = 20;
                return new Rect(posX, posY, width, height);
            }
            if (position.position == WindowPosition.BottomLeft) {
                var posX = 20;
                var posY = Screen.height - height - 20;
                return new Rect(posX, posY, width, height);
            }
            if (position.position == WindowPosition.TopRight) {
                var posX = Screen.width - width - 20;
                var posY = 20;
                return new Rect(posX, posY, width, height);
            }
            if (position.position == WindowPosition.BottomRight) {
                var posX = Screen.width - width - 20;
                var posY = Screen.height - height - 20;
                return new Rect(posX, posY, width, height);
            } else {
                var posX = (Screen.width - width) / 2f;
                var posY = (Screen.height - height) / 2f;
                return new Rect(posX, posY, width, height);
            }
        }

        public enum WindowPosition {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }

        public readonly struct WindowPositioning {
            public readonly WindowPosition position;
            public readonly float heightPercentage;
            public readonly float widthPercentage;

            public WindowPositioning(WindowPosition position, float heightPercentage = 0.45f, float widthPercentage = 0.2f) {
                this.position = position;
                this.heightPercentage = heightPercentage;
                this.widthPercentage = widthPercentage;
            }

            public static implicit operator WindowPositioning(WindowPosition position) {
                return new WindowPositioning(position);
            }
        }
    }
}
