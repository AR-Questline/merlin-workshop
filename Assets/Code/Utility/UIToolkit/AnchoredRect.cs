using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.Utils {
    public class AnchoredRect {
        public AnchoredPoint AnchoredPoint { get; private set; }
        
        public float X { get; }
        public float Y { get; }
        public StyleLength Width { get; }
        public StyleLength Height { get; }

        [UnityEngine.Scripting.Preserve] 
        public static AnchoredRect Default(StyleLength width, StyleLength height) => new (0, 0, width, height);
        
        public AnchoredRect(float xOffset, float yOffset, StyleLength width, StyleLength height, AnchoredPoint anchoredPoint = AnchoredPoint.TopLeft) {
            X = xOffset;
            Y = yOffset;
            Width = width;
            Height = height;
            AnchoredPoint = anchoredPoint;
        }
    }
    
    public enum AnchoredPoint {
        TopLeft,
        TopRight,
        TopCenter,
        BottomLeft,
        BottomRight,
        BottomCenter,
        CenterLeft,
        CenterRight,
        Center
    }
}