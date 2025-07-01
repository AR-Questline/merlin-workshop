using System.Linq;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Objectives.Trackers;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Editor.Utility.Localization {
    /// <summary>
    /// LocStringDrawer is used for inspector properties, in NodeGraphs LocStrings are drawn by NodeGuiUtil
    /// </summary>
    [CustomPropertyDrawer(typeof(LocString))]
    public class LocStringDrawer : PropertyDrawer {

        static string s_currentText;
        static StringTable OverridesTable => LocalizationSettings.StringDatabase.GetTable(LocalizationHelper.OverridesTable, LocalizationSettings.ProjectLocale);
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.serializedObject.isEditingMultipleObjects) {
                EditorGUI.LabelField(position, label.text, "LocStrings do not support multi-editing");
                return;
            }
            var rects = new PropertyDrawerRects(position);
            EditorGUI.PrefixLabel(rects.AllocateLine(), label);
            
            // get text field
            LocString locString = (LocString) property.GetPropertyValue();
            string fallbackString = locString.Fallback;
            var stringCollection = LocalizationUtils.DetermineStringTable(property);
            var stringTable = stringCollection?.GetTable(LocalizationSettings.ProjectLocale.Identifier) as StringTable;
            
            // validate term 
            string correctId = ValidateTerm(property, locString, stringTable, out bool hasIdChanged);

            bool isOverriden = !string.IsNullOrWhiteSpace(locString.IdOverride);
            string id = isOverriden ? locString.IdOverride : locString.ID;
            var entry = LocalizationHelper.GetTableEntry(id).entry;
            
            // validate category
            Category? category;
            
            // Hacky way of handling OptionalLocString
            var targetObject = property.serializedObject.targetObject;
            if (targetObject is ItemTemplate) {
                category = Category.Item;
            } else if (targetObject is QuestTemplateBase or ObjectiveSpecBase) {
                category = Category.Quest;
            } else if (targetObject is BaseTrackerAttachment) {
                category = Category.QuestTracker;
            } else {
                category = property.ExtractAttribute<LocStringCategoryAttribute>()?.Category;
            }
            
            if (entry != null && category != null) {
                var sharedMetadata = entry.SharedEntry.Metadata;
                var categoryMeta = sharedMetadata.GetMetadata<CategoryMetadata>();
                if (categoryMeta == null) {
                    categoryMeta = new CategoryMetadata();
                    sharedMetadata.AddMetadata(categoryMeta);
                }
                var validCategoryString = category.Value.ToString(); 
                bool wasChanged = categoryMeta.CategoryText != validCategoryString;
                categoryMeta.CategoryText = validCategoryString;

                if (wasChanged) {
                    EditorUtility.SetDirty(entry.Table.SharedData);
                }
            }
            
            // draw

            // --- Text Field with generated ID
            if (string.IsNullOrWhiteSpace(locString.IdOverride)) {
                string textString = LocalizationHelper.Translate(locString.ID, LocalizationSettings.ProjectLocale, true);
                float height = LocStringDrawerHelper.TextArea.CalcHeight(new GUIContent(textString), Screen.width);
                using var scope = new DisableGUIScope(correctId == null);
                EditableTextField(stringTable, locString.ID, textString, rects.AllocateTop(height + EditorGUIUtility.singleLineHeight));
            } else {
                LocalizationUtils.RemoveTableEntry(locString.ID, stringTable);
            }

            if (correctId == null) {
                using (new ColorGUIScope(Color.red)) {
                    EditorGUI.LabelField(rects.AllocateLine(), "Save required before editing!");
                }
            } else if (TGEditorPreferences.Instance.showTerms) {
                if (GUI.Button(rects.AllocateLeft(25), LocStringDrawerHelper.CopyIcon)) {
                    GUIUtility.systemCopyBuffer = locString.ID;
                }
                EditorGUI.LabelField(rects.AllocateLine(), locString.ID);
            }

            // --- Text Field with overriden ID
            if (isOverriden) {
                if (entry != null) {
                    Locale locale = LocalizationSettings.ProjectLocale;
                    string translation = LocalizationHelper.Translate(locString.IdOverride, locale, true);
                    float height = LocStringDrawerHelper.TextArea.CalcHeight(new GUIContent(translation), Screen.width);
                    EditableTextField(OverridesTable, locString.IdOverride, translation, rects.AllocateTop(height));
                } else {
                    float height = LocStringDrawerHelper.TextArea.CalcHeight(new GUIContent(fallbackString), Screen.width);
                    string value = EditorGUI.TextArea(rects.AllocateTop(height), fallbackString, LocStringDrawerHelper.TextArea);
                    locString.SetFallback(value, true);
                }
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            locString.IdOverride = EditorGUI.TextField(rects.AllocateLine(), "ID Override:", locString.IdOverride);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            DrawIdOverrideEntryButton(locString, fallbackString, entry != null);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            
            void EditableTextField(StringTable table, string entryId, string translation, Rect rect) {
                EditorGUI.BeginChangeCheck();
                string value = EditorGUI.TextArea(rect, translation, LocStringDrawerHelper.TextArea);
                if (EditorGUI.EndChangeCheck() || hasIdChanged) {
                    value = value?.Replace("\r", "");
                    if (table != null) {
                        if (!string.IsNullOrWhiteSpace(value)) {
                            LocalizationUtils.ChangeTextTranslation(entryId, value, table, true);
                        } else {
                            LocalizationUtils.RemoveTableEntry(entryId, table);
                        }
                    } else {
                        Log.Important?.Error($"Failed to find StringTable: {stringCollection}");
                    }
                }
            }
        }

        static string ValidateTerm(SerializedProperty property, LocString locString, StringTable stringTable, out bool wasChanged) {
            string oldId = locString.ID;
            wasChanged = LocalizationUtils.InspectorValidateTerm(property, LocalizationUtils.DetermineCategory(property), out string correctId);
            if (wasChanged) {
                locString.ID = correctId;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                string guid = "";
                if (!string.IsNullOrWhiteSpace(oldId)) {
                    guid = oldId.Split('_').Last();
                }
                var oldAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                // if ID was changed and old entry exists remove it
                if (stringTable != null && string.IsNullOrWhiteSpace(oldAssetPath)) {
                    LocalizationUtils.RemoveTableEntry(oldId, stringTable);
                }
            }

            return correctId;
        }

        void DrawIdOverrideEntryButton(LocString text, string tempString, bool entryExists) {
            if (string.IsNullOrWhiteSpace(text.IdOverride)) {
                return;
            }
            GUIStyle buttonStyle = new(EditorStyles.miniButton) {
                richText = true
            };
            if (!entryExists && GUILayout.Button("<b><color=#FF0000>ID Override doesn't exist</color></b> - Add new entry with typed in text!", buttonStyle)) {
                LocalizationUtils.ChangeTextTranslation(text.IdOverride, tempString, OverridesTable, true);
            }
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (property.serializedObject.isEditingMultipleObjects) {
                return EditorGUIUtility.singleLineHeight;
            }
            
            LocString text = (LocString) property.GetPropertyValue();
            string textString;
            if (string.IsNullOrWhiteSpace(text.IdOverride)) {
                textString = text + "\n";
            } else {
                Locale locale = LocalizationSettings.ProjectLocale;
                textString =  LocalizationHelper.Translate(text.IdOverride, locale, true);
            }
            GUIContent content = new(textString);
            float height = LocStringDrawerHelper.TextArea.CalcHeight(content, Screen.width);
            if (TGEditorPreferences.Instance.showTerms) {
                height += EditorGUIUtility.singleLineHeight;
            }
            // --- Single line for: PrefixLabel, OverridenId Label
            return height + EditorGUIUtility.singleLineHeight * 2f;
        }
    }

    static class LocStringDrawerHelper {
        static GUIStyle s_textArea;
        public static GUIStyle TextArea => s_textArea ?? (s_textArea = new GUIStyle(EditorStyles.textArea) {wordWrap = true});
        public static readonly GUIContent CopyIcon = new GUIContent("⇒");
    }
}
