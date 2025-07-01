using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Utility;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using PrefabUtil = Awaken.TG.Editor.Utility.PrefabUtil;

namespace Awaken.TG.Editor.MVC {
    [CustomEditor(typeof(ViewComponent), true)]
    public class ViewComponentEditor : OdinEditor {
        ViewComponent Target => target as ViewComponent;
        string _textToApply = string.Empty;
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            if (!PrefabUtil.IsInPrefabStage(Target.gameObject)) {
                return;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localization Prefix:", Target.LocID);
            EditorGUILayout.BeginHorizontal();
            _textToApply = EditorGUILayout.TextArea(_textToApply, EditorStyles.textArea);
            if (GUILayout.Button("Validate Terms")) {
                ApplyLabel(_textToApply);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Lazy Assets:");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load all")) {
                var allLazyImages = Target.gameObject.GetComponentsInChildren<LazyImage>(true);
                allLazyImages.ForEach(LazyImageEditor.Load);
            }
            if (GUILayout.Button("Unload all")) {
                var allLazyImages = Target.gameObject.GetComponentsInChildren<LazyImage>(true);
                allLazyImages.ForEach(LazyImageEditor.Unload);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // === Methods
        void ApplyLabel(string textToApply) {
            if (string.IsNullOrWhiteSpace(textToApply)) {
                textToApply = Target.LocID;
            } else {
                Target.LocID = textToApply;
            }
            UpdateViewLocalizationTerms(textToApply);
            _textToApply = string.Empty;
        }

        void UpdateViewLocalizationTerms(string textToApply) {
            LocalizationUtils.ValidateViewTerms(textToApply, Target.gameObject);
        }
    }
}