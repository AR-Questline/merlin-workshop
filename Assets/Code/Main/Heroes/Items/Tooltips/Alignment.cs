using System;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public enum Alignment : byte {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight,
    }

    public static class AlignmentUtil {
        public static Vector2 Pivot(this Alignment alignment) {
            return alignment switch {
                Alignment.UpperLeft => new Vector2(0, 1),
                Alignment.UpperCenter => new Vector2(0.5f, 1),
                Alignment.UpperRight => new Vector2(1, 1),
                Alignment.MiddleLeft => new Vector2(0, 0.5f),
                Alignment.MiddleCenter => new Vector2(0.5f, 0.5f),
                Alignment.MiddleRight => new Vector2(1, 0.5f),
                Alignment.LowerLeft => new Vector2(0, 0),
                Alignment.LowerCenter => new Vector2(0.5f, 0),
                Alignment.LowerRight => new Vector2(1, 0),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}