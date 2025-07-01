using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Tags;
using Awaken.Utility;
using Awaken.Utility.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorChangeItemsQuantity))]
    public class SChangeItemsQuantityEditor : ElementEditor {
        const string SpecifiedItems = nameof(SEditorChangeItemsQuantity.itemTemplateReferenceQuantityPairs);
        const string Tags = nameof(SEditorChangeItemsQuantity.tags);
        const string TaggedQuantity = nameof(SEditorChangeItemsQuantity.taggedQuantity);
        const string ForbiddenTags = nameof(SEditorChangeItemsQuantity.forbiddenTags);
        const string TagsManualSelection = nameof(SEditorChangeItemsQuantity.manualSelection);
        const string LootTable = nameof(SEditorChangeItemsQuantity.lootTableReference);
        const string TagsAllowCancel = nameof(SEditorChangeItemsQuantity.allowCancel);

        SEditorChangeItemsQuantity _step;
        bool _assignedFoldout;
        bool _forbiddenTagsFoldout;
        Mode _mode;
        
        static string TemplatesTooltip => "Use this to change quantity of specific items";
        static string TagsTooltip => "Use this to change quantity of items with given tags";
        static string ForbiddenTagsTooltip => "Use this to forbid items from having given tags.";
        static string LootTableTooltip => "Give items from loot table.";
        
        bool HasTemplateSettings => _step.itemTemplateReferenceQuantityPairs.Any();
        bool HasTagsSettings => _step.tags.Length > 0 || _step.forbiddenTags.Length > 0;
        bool HasLootTableSettings => _step.lootTableReference != null && _step.lootTableReference.IsSet;

        protected override void OnStartGUI() {
            base.OnStartGUI();
            _step = Target<SEditorChangeItemsQuantity>();
            
            if (HasLootTableSettings) {
                _mode = Mode.LootTable;
            } else if (HasTagsSettings) {
                _mode = Mode.Tags;
            } else {
                _mode = Mode.Templates;
            }
        }

        protected override void OnElementGUI() {
            DrawModeToolbar();
            CustomDrawProperties();

            if (_step.taggedQuantity == -1 && _step.allowCancel) {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Cancel");
                NodeEditorGUILayout.PortField(new GUIContent("Cancel"), target.TargetPort(), GUILayout.Width(0));
                GUILayout.EndHorizontal();
            }
        }

        void DrawModeToolbar() {
            // int settingModesSet = 0;
            // settingModesSet += HasLootTableSettings ? 1 : 0;
            // settingModesSet += HasTagsSettings ? 1 : 0;
            // settingModesSet += HasTemplateSettings ? 1 : 0;
            // if (settingModesSet > 1) {
            //     SirenixEditorGUI.WarningMessageBox("You have settings set in different modes. This should be supported. " +
            //                                        "However, please make sure that it is intended.");
            // }
            
            // SirenixEditorGUI.BeginHorizontalToolbar();
            foreach (Mode value in Enum.GetValues(typeof(Mode))) {
                bool isActive = _mode == value;
                string tooltip = value switch {
                    Mode.Templates => TemplatesTooltip,
                    Mode.Tags => TagsTooltip,
                    Mode.LootTable => LootTableTooltip,
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
                case Mode.LootTable:
                    DrawLootTables();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            DrawHorizontalLine();
            DrawProperties(nameof(SChangeItemsQuantity.isKnown));

            GUIUtils.PopLabelWidth();
        }

        void DrawTemplates() {
            DrawProperties(SpecifiedItems);
            bool hasAnyTemplate = _step.itemTemplateReferenceQuantityPairs.Any(p => p.itemTemplateReference?.IsSet ?? false);
            if (!hasAnyTemplate) {
                return;
            }
            DrawAssignedItems();
            
            bool hasAnyNegativeQuantity = _step.itemTemplateReferenceQuantityPairs.Any(p => p.quantity < 0);
            if (hasAnyNegativeQuantity) {
                DrawProperties("ignoreRequirements");
                DrawProperties("removeAll");
                DrawProperties("onlyStolen");
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

            bool hasTags = _step.tags?.Any(t => !string.IsNullOrWhiteSpace(t)) ?? false;
            bool hasForbiddenTags = _step.forbiddenTags?.Any(t => !string.IsNullOrWhiteSpace(t)) ?? false;

            if (hasTags || hasForbiddenTags) {
                DrawProperties(TagsManualSelection);
            }

            DrawProperties(TaggedQuantity);

            if (_step.taggedQuantity == -1) {
                DrawProperties(TagsAllowCancel);
            }

            if (_step.taggedQuantity <= -1) {
                DrawProperties("ignoreRequirements");
                DrawProperties("removeAll");
                DrawProperties("onlyStolen");
            }
        }

        void DrawLootTables() {
            DrawProperties(LootTable);
            DrawProperties("onlyStolen");
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
            string[] tags = Target<SEditorChangeItemsQuantity>().tags;
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

        void DrawAssignedItems() {
            _assignedFoldout = EditorGUILayout.Foldout(_assignedFoldout, "Show assigned items", true);
            if (_assignedFoldout) {
                foreach (var item in _step.itemTemplateReferenceQuantityPairs) {
                    if (item?.itemTemplateReference != null && item.itemTemplateReference.IsSet) {
                        ItemTemplate itemTemplate = item.ItemTemplate(null);
                        if (GUILayout.Button(itemTemplate.name)) {
                            Selection.activeObject = itemTemplate;
                        }
                    }
                }
            }
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
            LootTable
        }
    }
}