using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Localizations {
    public class PopupWindow : EditorWindow {
        const int Width = 650;
        const int MaxHeightOffset = 500; 
        static Vector2 scrollPosition;
        static string s_text;
        static string s_buttonText;
        static string s_alternateButton;
        static Action s_action;
        
        static GUIStyle s_label;
        
        static GUIStyle LabelStyle => s_label ??= new GUIStyle(EditorStyles.label) {
            richText = true,
            alignment = TextAnchor.MiddleCenter
        };
        
        public static void DisplayDialog(string title, string text, string buttonText, string alternateButton = "", Action alternateAction = null) {
            s_text = text;
            s_buttonText = buttonText;
            s_alternateButton = alternateButton;
            s_action = alternateAction;
            
            var window = GetWindow<PopupWindow>();
            window.titleContent = new GUIContent(title);
        
            var position = window.position;
            var height = LabelStyle.CalcHeight(new GUIContent(s_text), Width) + EditorStyles.miniButton.CalcHeight(new GUIContent(s_buttonText), Width) * 2 + EditorGUIUtility.singleLineHeight;
            if (height > Screen.currentResolution.height - MaxHeightOffset) {
                height = Screen.currentResolution.height - MaxHeightOffset;
            }
            position.width = Width;
            position.height = height;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            window.position = position;
            
            window.ShowModal();
        }
        
        void OnGUI() {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label(s_text, LabelStyle);
            GUILayout.EndScrollView();
            
            if (GUILayout.Button(s_buttonText)) {
                Close();
            }

            if (!string.IsNullOrWhiteSpace(s_alternateButton) && GUILayout.Button(s_alternateButton)) {
                s_action?.Invoke();
                Close();
            }
        }
    }
}