#if !UNITY_2021_2_OR_NEWER
using System;

namespace UnityEditor.Localization.Bridge
{
    static class EditorGUIBridge
    {
        public static event EventHandler hyperLinkClicked
        {
            add => EditorGUI.hyperLinkClicked += value;
            remove => EditorGUI.hyperLinkClicked -= value;
        }
    }
}
#endif
