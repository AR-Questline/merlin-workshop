using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.AI.Barks {
    public class GoogleSheetLinkWindow : EditorWindow {
        const string LastUsedLinkKey = "GoogleSheetLinkWindow_LastUsedLink";
        string _googleSheetLink = string.Empty;
        Action<string> _okButtonClickedAction;

        public static void ShowWindow(Action<string> onOkButtonClickedAction) {
            var window = GetWindow<GoogleSheetLinkWindow>(true, "Enter Google Sheet Link", true);
            window.ShowPopup();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 100);
            window._okButtonClickedAction = onOkButtonClickedAction;
            window._googleSheetLink = EditorPrefs.GetString(LastUsedLinkKey, string.Empty);
        }

        void OnGUI() {
            GUILayout.Label("Link", EditorStyles.boldLabel);
            _googleSheetLink = EditorGUILayout.TextField(_googleSheetLink, GUILayout.Width(position.width - 20));

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ok")) {
                OnOkButtonClicked();
            }

            if (GUILayout.Button("Cancel")) {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        void OnOkButtonClicked() {
            if (!string.IsNullOrEmpty(_googleSheetLink)) {
                EditorPrefs.SetString(LastUsedLinkKey, _googleSheetLink);
                _okButtonClickedAction?.Invoke(_googleSheetLink);
            } else {
                Debug.LogWarning("Google Sheet link is empty.");
            }

            Close();
        }
    }
}