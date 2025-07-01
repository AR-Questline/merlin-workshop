using Awaken.TG.Main.Utility.VSDatums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public abstract class VSDatumTypeInstanceDrawer {
        public abstract void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed);
        public abstract void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed);
    }
}