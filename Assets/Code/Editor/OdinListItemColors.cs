using System;
using System.Reflection;
using Awaken.TG.Editor.Assets;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor {
    [InitializeOnLoad]
    public static class OdinListItemColors {
        static OdinListItemColors() {
            SetColors();
        }

        public static void SetColors() {
            // SirenixGUIStyles.ListItemColorEven = TGEditorPreferences.Instance.listItemColorEven;
            // SirenixGUIStyles.ListItemColorOdd = TGEditorPreferences.Instance.listItemColorOdd;
        }
    }
}
