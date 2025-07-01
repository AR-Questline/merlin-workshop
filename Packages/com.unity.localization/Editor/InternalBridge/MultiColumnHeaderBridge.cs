using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Localization.Bridge
{
    abstract class MultiColumnHeaderBridge : MultiColumnHeader
    {
        static readonly Color k_DividerColorDark = new Color(0, 0, 0, 1);
        static readonly Color k_DividerColorLight = new Color(0, 0, 0, 0.3f);

        public MultiColumnHeaderBridge(MultiColumnHeaderState state) : base(state)
        {
        }

        internal override void DrawDivider(Rect dividerRect, MultiColumnHeaderState.Column column)
        {
            // Remove the vertical padding from the divider rect
            dividerRect.y -= 4f;
            dividerRect.height += 8f;

            DrawDivider(dividerRect);
        }

        public static void DrawDivider(Rect dividerRect)
        {
            EditorGUI.DrawRect(dividerRect, EditorGUIUtility.isProSkin ? k_DividerColorDark : k_DividerColorLight);
        }
    }
}
