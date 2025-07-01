using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    public class TopToolbarButton : ITopToolbarElement {
        readonly Action _action;
        readonly int _defaultWidth;
        GUIContent _guiContent;
        int _width;
        GUILayoutOption _widthOption;

        public string Name { get; }
        public bool DefaultEnabled { get; }
        public bool CanChangeSide => true;
        public TopToolbarButtons.Side DefaultSide { get; }

        public IEnumerable<string> CustomKeys { get; }

        string WidthKey => $"{((ITopToolbarElement)this).MainKey}.Width";

        public TopToolbarButton(string name, string tooltip, Action action, int width,
            TopToolbarButtons.Side defaultSide, bool defaultEnabled) {
            Name = name;
            CustomKeys = new[] {
                WidthKey,
            };
            DefaultEnabled = defaultEnabled;
            DefaultSide = defaultSide;
            _guiContent = new GUIContent(((ITopToolbarElement)this).ShowName, tooltip);
            _action = action;
            _defaultWidth = width;
            AfterResetPrefsBasedValues();
        }

        public void SettingsGUI() {
            EditorGUILayout.LabelField("Width", GUILayout.Width(ITopToolbarElement.DefaultLabelWidth));
            EditorGUI.BeginChangeCheck();
            _width = EditorGUILayout.IntSlider("", _width, 0, 150);
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetInt(WidthKey, _width);
                _widthOption = GUILayout.Width(_width);
            }
        }

        public void AfterResetPrefsBasedValues() {
            _width = EditorPrefs.GetInt(WidthKey, _defaultWidth);
            _widthOption = GUILayout.Width(_width);
        }

        public void OnGUI() {
            _guiContent.text = ((ITopToolbarElement)this).ShowName;
            if (GUILayout.Button(_guiContent, EditorStyles.toolbarButton, _widthOption)) {
                _action();
            }
        }
    }
}
