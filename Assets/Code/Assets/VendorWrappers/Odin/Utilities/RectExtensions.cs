using UnityEngine;

namespace Sirenix.Utilities
{
    public static class RectExtensions
    {
        public static Rect Expand(this Rect rect, float expand)
        {
            rect.x -= expand;
            rect.y -= expand;
            rect.height += expand * 2f;
            rect.width += expand * 2f;
            return rect;
        }
    }
}