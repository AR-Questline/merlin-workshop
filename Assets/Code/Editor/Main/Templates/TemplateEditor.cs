using System.Linq;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.Extensions;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Templates {
    [CustomEditor(typeof(Template), true), CanEditMultipleObjects]
    public class TemplateEditor : BasePresetsEditor {
        SerializedProperty _isAbstract;
        SerializedProperty _templateType;
        ItemsInGameCache _itemsInGameCache;
        NpcCache _npcCache;
        LocationCache _locationCache;

        string _lastGuidCacheBake;
        bool _isInGuidCache;
        bool _shouldShowWarningItemsInGameCache;
        bool _shouldShowWarningNPCCache;
        bool _shouldShowWarningLocationCache;

        protected override void OnEnable() {
            base.OnEnable();
            _isAbstract = serializedObject.FindProperty("_isAbstract");
            _templateType = serializedObject.FindProperty("templateType");
            if (GUIDCache.Instance == null) {
                GUIDCache.Load();
            }

            _lastGuidCacheBake = GUIDCache.Instance.LastBake;
            _isInGuidCache = !GUIDCache.Instance.IsUnused(target);

            if (_isInGuidCache) {
                _itemsInGameCache = ItemsInGameCache.Get;
                _shouldShowWarningItemsInGameCache = target is ItemTemplate itemTemplate && !_itemsInGameCache.Editor_HasAnyOccurrencesOf(itemTemplate);
                if (!_shouldShowWarningLocationCache) {
                    _npcCache = NpcCache.Get;
                    _shouldShowWarningNPCCache = target is NpcTemplate npcTemplate && !_npcCache.HasAnyOccurenceOf(npcTemplate);
                    if (!_shouldShowWarningNPCCache) {
                        _locationCache = LocationCache.Get;
                        _shouldShowWarningLocationCache = target is LocationTemplate locationTemplate && !_locationCache.HasAnyOccurrencesOf(locationTemplate);
                    }
                }
            }
        }

        public override void OnInspectorGUI() {
            float prevWidth = EditorGUIUtility.labelWidth;
            TemplateType trueTemplateType = ((ITemplate)target).TemplateType;
            bool isTypeOverriden = trueTemplateType != (TemplateType)_templateType.enumValueIndex;

            if (!_isInGuidCache) {
                DrawNoneUsageWarning($"Based on GUID CACHE from {_lastGuidCacheBake} this template is not used anywhere relevant.");
            } else {
                if (_shouldShowWarningItemsInGameCache) {
                    DrawNoneUsageInfo($"Based on ITEMS IN GAME CACHE from {_itemsInGameCache.LastBake} this template cannot be found in the game.");
                } else if (_shouldShowWarningNPCCache) {
                    DrawNoneUsageWarning($"Based on NPC CACHE from {_npcCache.LastBake} this npc does not spawn anywhere in the world.");
                } else if (_shouldShowWarningLocationCache) {
                    DrawNoneUsageWarning($"Based on LOCATION CACHE from {_locationCache.LastBake} this location does not spawn anywhere in the world.");
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUIUtility.labelWidth = 100;

                EditorGUILayout.PropertyField(_isAbstract, GUILayout.ExpandWidth(false));
                GUILayout.Space(10);

                EditorGUIUtility.labelWidth = 70;
                GUILayout.Label(new GUIContent("Template Type"));
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(_templateType, GUIContent.none, GUILayout.ExpandWidth(true));
                if (isTypeOverriden) {
                    GUILayout.Space(5);
                    GUILayout.Label(new GUIContent("Overriden to: " + trueTemplateType.ToStringFast()));
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUIUtility.labelWidth = prevWidth;

            bool disableGroupStarted = false;
            if (trueTemplateType == TemplateType.ForRemoval) {
                EditorGUI.BeginDisabledGroup(true);
                disableGroupStarted = true;
            }

            base.OnInspectorGUI();

            if (disableGroupStarted) {
                EditorGUI.EndDisabledGroup();
            }
        }

        void DrawNoneUsageWarning(string message) {
            EditorGUILayout.BeginHorizontal();
            // SirenixEditorGUI.WarningMessageBox(message);
            if (GUILayout.Button(new GUIContent("Mark for removal", "Sets TemplateType of this object to 'TemplateType.ForRemoval'"))) {
                _templateType.SetValue(TemplateType.ForRemoval);
            }

            EditorGUILayout.EndHorizontal();
        }
        
        void DrawNoneUsageInfo(string message) {
            EditorGUILayout.BeginHorizontal();
            // SirenixEditorGUI.InfoMessageBox(message);
            if (GUILayout.Button(new GUIContent("Mark for removal", "Sets TemplateType of this object to 'TemplateType.ForRemoval'"))) {
                _templateType.SetValue(TemplateType.ForRemoval);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}