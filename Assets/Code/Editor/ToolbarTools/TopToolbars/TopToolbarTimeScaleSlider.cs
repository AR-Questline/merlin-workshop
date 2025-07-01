using System.Collections.Generic;
using System.Globalization;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools.TopToolbars {
    public class TopToolbarTimeScaleSlider : ITopToolbarElement {
        static readonly GUILayoutOption NotExpandWidthOption = GUILayout.ExpandWidth(false);

        int _slidersWidth;
        GUILayoutOption _slidersWidthOption;

        bool _showValueLabel;
        int _labelWidth;
        GUILayoutOption _labelWidthOption;

        public string Name => "Time Scale";
        public bool CanChangeSide => true;
        public bool DefaultEnabled => true;
        public TopToolbarButtons.Side DefaultSide => TopToolbarButtons.Side.Left;
        public IEnumerable<string> CustomKeys { get; }

        string SliderWidthKey => $"{((ITopToolbarElement)this).MainKey}.SliderWidth";
        string ShowValueLabelKey => $"{((ITopToolbarElement)this).MainKey}.ShowValueLabel";
        string LabelWidthKey => $"{((ITopToolbarElement)this).MainKey}.LabelWidth";

        public TopToolbarTimeScaleSlider() {
            CustomKeys = new[] {
                SliderWidthKey,
                ShowValueLabelKey,
                LabelWidthKey,
            };

            AfterResetPrefsBasedValues();
        }

        public void SettingsGUI() {
            EditorGUILayout.LabelField("Slider Width", GUILayout.Width(ITopToolbarElement.DefaultLabelWidth));
            EditorGUI.BeginChangeCheck();
            _slidersWidth = EditorGUILayout.IntSlider("", _slidersWidth, 80, 250, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetInt(SliderWidthKey, _slidersWidth);
                _slidersWidthOption = GUILayout.Width(_slidersWidth/2);
            }

            EditorGUILayout.LabelField("Value label", GUILayout.Width(ITopToolbarElement.DefaultLabelWidth));
            EditorGUI.BeginChangeCheck();
            _showValueLabel = EditorGUILayout.Toggle("", _showValueLabel, GUILayout.Width(15));
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetBool(ShowValueLabelKey, _showValueLabel);
            }

            EditorGUILayout.LabelField("Value Width", GUILayout.Width(ITopToolbarElement.DefaultLabelWidth));
            EditorGUI.BeginChangeCheck();
            _labelWidth = EditorGUILayout.IntSlider("", _labelWidth, 0, 100, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetInt(LabelWidthKey, _labelWidth);
                _labelWidthOption = GUILayout.Width(_labelWidth);
            }
        }

        public void AfterResetPrefsBasedValues() {
            _slidersWidth = EditorPrefs.GetInt(SliderWidthKey, 150);
            _slidersWidthOption = GUILayout.Width(_slidersWidth/2);

            _showValueLabel = EditorPrefs.GetBool(ShowValueLabelKey, true);

            _labelWidth = EditorPrefs.GetInt(LabelWidthKey, 50);
            _labelWidthOption = GUILayout.Width(_labelWidth);
        }

        public void OnGUI() {
            GUILayout.Label(((ITopToolbarElement)this).ShowName, NotExpandWidthOption);
            float timeScale = Time.timeScale;
            timeScale = GUILayout.HorizontalSlider(timeScale, 0, 1, _slidersWidthOption);
            GUILayout.Space(-13);
            timeScale = GUILayout.HorizontalSlider(timeScale, 1, 10, _slidersWidthOption);

            if (_showValueLabel) {
                string textFieldResult = GUILayout.TextField(timeScale.ToString("f2", CultureInfo.InvariantCulture), _labelWidthOption);
                if (float.TryParse(textFieldResult, out float newTimeScale)) {
                    timeScale = newTimeScale;
                }
            }

            if (timeScale != Time.timeScale) {
                World.Any<GlobalTime>()?.DEBUG_SetTimeScale(timeScale);
            }
        }
    }
}
