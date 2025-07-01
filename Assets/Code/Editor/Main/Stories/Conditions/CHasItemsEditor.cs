using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Tags;
using Awaken.Utility;
using Awaken.Utility.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Editor.Main.Stories.Conditions {
    [CustomElementEditor(typeof(CEditorHasItems))]
    public class CHasItemsEditor : ElementEditor {
        const string SpecifiedItems = nameof(CEditorHasItems.requiredItemTemplateReferenceQuantityPairs);
        const string Tags = nameof(CEditorHasItems.tags);
        const string TaggedQuantity = nameof(CEditorHasItems.tagsQuantity);
        const string ForbiddenTags = nameof(CEditorHasItems.forbiddenTags);
        const string OnlyStolen = nameof(CEditorHasItems.onlyStolen);
        const string OnlyEquipped = nameof(CEditorHasItems.onlyEquipped);

        CEditorHasItems _condition;
        bool _assignedFoldout;
        bool _forbiddenTagsFoldout; 
        Mode _mode;
        
        static string TemplatesTooltip => "Use this to require specific items";
        static string TagsTooltip => "Use this to require items with specific tags.";
        static string ForbiddenTagsTooltip => "Use this to prohibit items with the specified tags.";

        bool HasTemplateSettings => _condition.requiredItemTemplateReferenceQuantityPairs.Any();
        bool HasTagsSettings => _condition.tags.Length > 0 || _condition.forbiddenTags.Length > 0;

        protected override void OnStartGUI() {
            base.OnStartGUI();
            _condition = Target<CEditorHasItems>();
            _mode = HasTemplateSettings ? Mode.Templates : Mode.Tags;
        }

        protected override void OnElementGUI() {
            DrawModeToolbar();
            CustomDrawProperties();
        }

        void DrawModeToolbar() {
            int settingModesSet = 0;
            settingModesSet += HasTagsSettings ? 1 : 0;
            settingModesSet += HasTemplateSettings ? 1 : 0;
            if (settingModesSet > 1) {
                // SirenixEditorGUI.WarningMessageBox("You have settings set in different modes. This should be supported. " +
                //                                    "However, please make sure that it is intended.");
            }

            // SirenixEditorGUI.BeginHorizontalToolbar();
            foreach (Mode value in Enum.GetValues(typeof(Mode))) {
                bool isActive = _mode == value;
                string tooltip = value switch {
                    Mode.Templates => TemplatesTooltip,
                    Mode.Tags => TagsTooltip,
                    _ => string.Empty
                };

                GUIContent content = new(value.ToString(), tooltip);
                // if (SirenixEditorGUI.ToolbarTab(isActive, content) && !isActive) {
                //     _mode = value;
                // }
            }

            // SirenixEditorGUI.EndHorizontalToolbar();
        }

        void CustomDrawProperties() {
            GUIUtils.PushLabelWidth(150);

            GUILayout.Space(15f);
            switch (_mode) {
                case Mode.Templates:
                    DrawTemplates();
                    break;
                case Mode.Tags:
                    DrawTags();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            DrawHorizontalLine();

            GUIUtils.PopLabelWidth();
        }

        void DrawTemplates() {
            DrawProperties(SpecifiedItems);
            bool hasAnyTemplate = _condition.requiredItemTemplateReferenceQuantityPairs.Any(p => p.itemTemplateReference?.IsSet ?? false);
            if (!hasAnyTemplate) {
                return;
            }

            DrawAssignedItems();
            DrawProperties(OnlyStolen);
            DrawProperties(OnlyEquipped);
        }

        void DrawAssignedItems() {
            _assignedFoldout = EditorGUILayout.Foldout(_assignedFoldout, "Show assigned items", true);
            if (_assignedFoldout) {
                foreach (var item in _condition.requiredItemTemplateReferenceQuantityPairs) {
                    if (item?.itemTemplateReference != null && item.itemTemplateReference.IsSet) {
                        ItemTemplate itemTemplate = item.ItemTemplate(null);
                        if (GUILayout.Button(itemTemplate.name)) {
                            Selection.activeObject = itemTemplate;
                        }
                    }
                }
            }
        }

        void DrawTags() {
            DrawTagTranslationHint();
            DrawProperties(Tags);
            DrawHorizontalLine();
            
            _forbiddenTagsFoldout = EditorGUILayout.Foldout(_forbiddenTagsFoldout, new GUIContent("Forbidden Tags", ForbiddenTagsTooltip), true);
            if (_forbiddenTagsFoldout) {
                DrawTagTranslationHint();
                DrawProperties(ForbiddenTags);
            }
            DrawHorizontalLine();
            
            DrawProperties(TaggedQuantity);
            DrawProperties(OnlyStolen);
            DrawProperties(OnlyEquipped);
            
        }

        void DrawTagTranslationHint() {
            if (CheckTagsTranslations(out List<string> notFoundTags)) {
                EditorGUILayout.HelpBox("At least one tag ID doesn't exist", MessageType.Error);
                if (GUILayout.Button("Add missing entries to localization table",
                        new GUIStyle(EditorStyles.miniButton) { richText = true })) {
                    foreach (var tag in notFoundTags) {
                        string id = TagUtils.GetTagID(tag);
                        LocalizationUtils.ChangeTextTranslation(id, tag,
                            LocalizationSettings.StringDatabase.GetTable(LocalizationHelper.TagsTable,
                                LocalizationSettings.ProjectLocale));
                    }
                }
            }
        }
        
        bool CheckTagsTranslations(out List<string> notFoundTags) {
            string[] tags = Target<CEditorHasItems>().tags;
            notFoundTags = new List<string>();
            if (tags == null || tags.Length < 1) {
                return false;
            }

            foreach (string tag in tags) {
                string id = TagUtils.GetTagID(tag);
                if (LocalizationHelper.GetTableEntry(id).entry == null && !notFoundTags.Contains(tag)) {
                    notFoundTags.Add(tag);
                }
            }

            return notFoundTags.Count > 0;
        }
        
        void DrawHorizontalLine() {
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(12f));
            EditorGUI.indentLevel = indent;
        }

        enum Mode : byte {
            Templates,
            Tags,
        }
    }
}