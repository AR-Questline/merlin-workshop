using System;
using System.Globalization;
using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class ColorUtils {
        public static Color WithAlpha(this Color color, float alpha) {
            color.a = alpha;
            return color;
        }

        public static Color MoveTowards(this Color color, Color other, float t) {
            return Color.Lerp(color, other, t);
        }

        public static Color HexToColor(string hex) {
            return HexToColor(hex.AsSpan());
        }

        public static Color HexToColor(ReadOnlySpan<char> hex) {
            if (hex[0] == '#') {
                hex = hex[1..];
            }
            var hasAlpha = hex.Length > 6;
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var intValue)) {
                return Color.magenta;
            }
            var color = new Color();
            var shiftValue = hasAlpha ? 24 : 16;
            for (var i = 0; i < 3; i++) {
                color[i] = ((intValue >> shiftValue) & 255) / 255f;
                shiftValue -= 8;
            }
            if (hasAlpha) {
                color.a = (intValue & 255) / 255f;
            } else {
                color.a = 1f;
            }

            return color;
        }

        public static string ToHex(this Color color) {
            return $"#{(int)(color.r*255):X2}{(int)(color.g*255):X2}{(int)(color.b*255):X2}";
        }
    }
}