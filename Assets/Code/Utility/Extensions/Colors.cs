using UnityEngine;

namespace Awaken.Utility.Extensions {
    public static class Colors {
        public static System.Drawing.Color ToSystemColor(this Color color) {
            return ((Color32)color).ToSystemColor();
        }
        
        public static System.Drawing.Color ToSystemColor(this Color32 color) {
            return System.Drawing.Color.FromArgb(color.a, color.r, color.g, color.b);
        }
    }
}