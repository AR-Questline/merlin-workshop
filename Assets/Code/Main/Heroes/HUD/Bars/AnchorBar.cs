using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Bars {
    public class AnchorBar : SimpleBar {
        [SerializeField] Axis axis;
        [SerializeField, ShowIf(nameof(IsAnchorX)), LabelText("origin")] OriginX originX;
        [SerializeField, ShowIf(nameof(IsAnchorY)), LabelText("origin")] OriginY originY;

        [SerializeField] RectTransform anchorSocket;

        public override void SetPercent(float percent) {
            Color = Color.WithAlpha(percent == 0 ? 0 : 1);
            if (axis == Axis.X) {
                if (originX == OriginX.Left) {
                    anchorSocket.anchorMin = new Vector2(0, 0);
                    anchorSocket.anchorMax = new Vector2(percent, 1);
                } else {
                    anchorSocket.anchorMin = new Vector2(1 - percent, 0);
                    anchorSocket.anchorMax = new Vector2(1, 1);
                }
            } else {
                if (originY == OriginY.Down) {
                    anchorSocket.anchorMin = new Vector2(0, 0);
                    anchorSocket.anchorMax = new Vector2(1, percent);
                } else {
                    anchorSocket.anchorMin = new Vector2(0, 1 - percent);
                    anchorSocket.anchorMax = new Vector2(1, 1);
                }
            }
        }

        enum Axis { X, Y }
        enum OriginX { Left, [UnityEngine.Scripting.Preserve] Right }
        enum OriginY { [UnityEngine.Scripting.Preserve] Up, Down }
        
        bool IsAnchorX => axis == Axis.X;
        bool IsAnchorY => axis == Axis.Y;
    }
}