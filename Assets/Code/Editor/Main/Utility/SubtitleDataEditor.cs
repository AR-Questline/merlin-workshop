using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Utility.Video.Subtitles;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Editor.Main.Utility {
    [CustomEditor(typeof(SubtitlesData))]
    public class SubtitleDataEditor : OdinEditor {

        List<string> _texts = new List<string>();
        
        Locale _localeToOverrideTime;
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Load from source")) {
                _texts.Clear();
                LoadFromAsset();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Time Override For") && _localeToOverrideTime != null) {
                AddTimeOverride(_localeToOverrideTime);
                _localeToOverrideTime = null;
            }
            _localeToOverrideTime = EditorGUILayout.ObjectField(_localeToOverrideTime, typeof(Locale), false) as Locale;
            GUILayout.EndHorizontal();
        }

        void LoadFromAsset() {
            var textAsset = ((SubtitlesData) target).source;
            if (textAsset == null) return;
            var blocks = SRTParser.Load(textAsset);
            ((SubtitlesData) target).records = blocks.Select(Extract).ToArray();
            serializedObject.Update();
            var records = serializedObject.FindProperty("records");
            for (int i = 0; i < records.arraySize; i++) {
                var record = records.GetArrayElementAtIndex(i);
                var text = record.FindPropertyRelative(nameof(SubtitlesData.Record.text));
                var id = text.FindPropertyRelative("ID").stringValue;
                if (LocalizationUtils.InspectorValidateTerm(text, LocalizationUtils.DetermineCategory(text), out var newTerm)) {
                    id = newTerm;
                }
                text.FindPropertyRelative("ID").stringValue = id;
                StringTableCollection stringTableCollection = LocalizationUtils.DetermineStringTable(text);
                LocalizationUtils.ChangeTextTranslation(id, _texts[i], stringTableCollection.GetTable(LocalizationSettings.ProjectLocale.Identifier) as StringTable);
            }

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        void AddTimeOverride(Locale locale) {
            var records = ((SubtitlesData)target).records;
            foreach (var record in records) {
                record.AddTimeOverride(locale);
            }
        }
        
        SubtitlesData.Record Extract(SubtitleBlock block) {
            var record = new SubtitlesData.Record((float) block.From, (float) block.To);
            _texts.Add(block.Text);
            return record;
        }
    }
}