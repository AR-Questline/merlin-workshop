using System;
using System.Collections.Generic;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    public class TopToolbarSettings : SettingsProvider {
        public const string PreferencesTgTopToolbarPath = "Preferences/TG/Top toolbar";

        static TopToolbarSettings s_instance;
        public static TopToolbarSettings Instance => s_instance ??= (TopToolbarSettings)CreateMyCustomSettingsProvider();

        public float ToolbarSpacing {
            get => EditorPrefs.GetFloat("TopToolbarSettings.toolbarSpacing", 5f);
            set => EditorPrefs.SetFloat("TopToolbarSettings.toolbarSpacing", value);
        }
        public float ToolbarMargin {
            get => EditorPrefs.GetFloat("TopToolbarSettings.toolbarMargin", 20f);
            set => EditorPrefs.SetFloat("TopToolbarSettings.toolbarMargin", value);
        }

        public TopToolbarSettings(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) {}

        public override void OnGUI(string searchContext) {
            var searchParts = !string.IsNullOrWhiteSpace(searchContext) ?
                searchContext.Split(' ') :
                Array.Empty<string>();

            EditorGUI.BeginChangeCheck();

            DrawSlider(searchParts, "Toolbar Spacing", ToolbarSpacing, 0, 20, f => ToolbarSpacing = f);
            DrawSlider(searchParts, "Toolbar Margin", ToolbarMargin, 0, 50, f => ToolbarMargin = f);

            var elements = TopToolbarButtons.Elements;
            bool structuralChange = false;
            EditorGUILayout.LabelField("Left Side:", EditorStyles.largeLabel);
            for (int i = 0; i < elements.Length; i++) {
                ITopToolbarElement element = elements[i];
                if (element.Side == TopToolbarButtons.Side.Left && element.HasSearchInterest(searchParts)) {
                    structuralChange = DrawElement(element) || structuralChange;
                }
            }

            EditorGUILayout.LabelField("Right Side:", EditorStyles.largeLabel);
            for (int i = 0; i < elements.Length; i++) {
                ITopToolbarElement element = elements[i];
                if (element.Side == TopToolbarButtons.Side.Right && element.HasSearchInterest(searchParts)) {
                    structuralChange = DrawElement(element) || structuralChange;
                }
            }

            if (GUILayout.Button("Reset to defaults")) {
                EditorPrefs.DeleteKey("TopToolbarSettings.toolbarSpacing");
                EditorPrefs.DeleteKey("TopToolbarSettings.toolbarMargin");
                foreach (var element in elements) {
                    foreach (var key in element.PrefsKeys) {
                        EditorPrefs.DeleteKey(key);
                    }
                    element.AfterResetPrefsBasedValues();
                }
                TopToolbarButtons.OnOrderReset();
            }

            if (structuralChange) {
                TopToolbarButtons.AssignSides();
            }

            if (EditorGUI.EndChangeCheck()) {
                RepaintToolbar();
            }

            base.OnGUI(searchContext);
        }
        
        static bool DrawElement(ITopToolbarElement element) {
            bool structuralChange = false;
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("▲", GUILayout.Width(20))) {
                element.Order -= 2;
                structuralChange = true;
            }
            
            if (GUILayout.Button("▼", GUILayout.Width(20))) {
                element.Order += 2;
                structuralChange = true;
            }
            
            using (var _ = new EditorGUI.DisabledGroupScope(!element.CanChangeSide)) {
                if (GUILayout.Button(element.Side.IsRight() ? "◀" : "▶", GUILayout.Width(20))) {
                    element.Side = element.Side.Other();
                    structuralChange = true;
                }
            }

            EditorGUI.BeginChangeCheck();
            var enabled = EditorGUILayout.Toggle("", element.Enabled, GUILayout.Width(15));
            if (EditorGUI.EndChangeCheck()) {
                element.Enabled = enabled;
            }

            EditorGUI.BeginChangeCheck();
            var showName = EditorGUILayout.TextArea(element.ShowName, GUILayout.Width(110));
            if (EditorGUI.EndChangeCheck()) {
                element.ShowName = showName;
            }
            
            element.SettingsGUI();
            
            EditorGUILayout.EndHorizontal();
            return structuralChange;
        }

        void RepaintToolbar() {
            ToolbarCallback.s_isDirty = true;
        }

        void DrawSlider(string[] searchParts, string label, float value, float min, float max, Action<float> write) {
            if (!label.ContainsAny(searchParts)) {
                return;
            }
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Slider(label, value, min, max);
            if (EditorGUI.EndChangeCheck()) {
                write(value);
            }
        }

        public override bool HasSearchInterest(string searchContext) {
            var searchParts = !string.IsNullOrWhiteSpace(searchContext) ?
                searchContext.Split(' ') :
                Array.Empty<string>();
            if ("Toolbar Spacing".ContainsAny(searchParts)) {
                return true;
            }
            if ("Toolbar Margin".ContainsAny(searchParts)) {
                return true;
            }
            var elements = TopToolbarButtons.Elements;
            foreach (var element in elements) {
                if (element.HasSearchInterest(searchParts)) {
                    return true;
                }
            }
            return base.HasSearchInterest(searchContext);
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider() {
            TopToolbarSettings provider = new TopToolbarSettings(PreferencesTgTopToolbarPath) {
                keywords = new HashSet<string>(new[] { "ar", "tg", "toolbar", "topToolbar" }),
            };
            return provider;
        }
    }
}
