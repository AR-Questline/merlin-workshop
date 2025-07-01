using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    public class TopToolbarSpace : ITopToolbarElement {
        public string Name => "Space";
        string ITopToolbarElement.MainKey => $"{nameof(ITopToolbarElement)}.{Name}_{DefaultSide}";
        public bool CanChangeSide => false;
        public bool DefaultEnabled => true;
        public TopToolbarButtons.Side DefaultSide { get; }

        public IEnumerable<string> CustomKeys => Array.Empty<string>();

        public TopToolbarSpace(TopToolbarButtons.Side defaultSide) {
            DefaultSide = defaultSide;
        }

        public void SettingsGUI() {
            EditorGUILayout.LabelField("Space", EditorStyles.boldLabel);
        }

        public void AfterResetPrefsBasedValues() {}

        public void OnGUI() {
            GUILayout.FlexibleSpace();
        }
    }
}
