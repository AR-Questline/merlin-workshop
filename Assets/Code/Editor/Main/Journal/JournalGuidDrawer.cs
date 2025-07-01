using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Memories.Journal.Conditions;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Journal {
    [CustomPropertyDrawer(typeof(JournalGuid))]
    public class JournalGuidDrawer : PropertyDrawer {
        // these are needed to handle the cases of duplicating and copying objects
        static readonly HashSet<Guid> Guids = new(20);
        static readonly HashSet<GuidPathPair> GUIDPairs = new(20);

        string _playerTypedValue;
        string _savedValue;
        
        #region CacheStateHandling
        // Might be possible to do this universally by saving previous selection
        // guids and current. purging the older selection guids
        
        [InitializeOnLoadMethod]
        static void InitPrefabStageHandler() {
            PrefabStage.prefabStageOpened += OnPrefabStageChanged;
            PrefabStage.prefabStageClosing += OnPrefabStageChanged;
        }

        static void OnPrefabStageChanged(PrefabStage obj) => Clear();

        static void Clear() {
            Guids.Clear();
            GUIDPairs.Clear();
        }

        #endregion

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            JournalGuid guid = (JournalGuid) property.boxedValue;
            if (property.ExtractAttribute<GuidSelectionAttribute>() != null) {
                HandleGUIDSelection(position, property, guid);
            } else {
                HandleGUIDPreview(position, property, guid);
            }
            EditorGUI.EndProperty();
        }

        void HandleGUIDSelection(Rect position, SerializedProperty property, JournalGuid guid) {
            // gather info for all conditions that use a guid for unlocking
            var condition = CommonReferences.Get.Journal.GetEntryDatas()
                                                   .SelectMany(d => {
                                                       string type = StringUtil.NicifyTypeName(d).Replace(" Data", "");
                                                       var conditionData = d.GetEntries()
                                                           .Where(data => data.Condition is Condition) //<- gdzies tu
                                                           .Select(data => {
                                                               return new {
                                                                   Guid = ((Condition)data.Condition).Guid,
                                                                   Folder = $"{type}/{d.EntryName}/",
                                                                   FriendlyName = data.ElementLabelText(),
                                                                   Combined = $"{type}/{d.EntryName}/{data.ElementLabelText()}"
                                                               };
                                                           });
                                                       if (d.conditionForEntry is Condition c) {
                                                           conditionData = conditionData.Append(new {
                                                               Guid = c.Guid,
                                                               Folder = $"{type}/{d.EntryName}/",
                                                               FriendlyName = d.EntryName,
                                                               Combined = $"{type}/{d.EntryName}/{d.EntryName}"
                                                           });
                                                       }
                                                       return conditionData;
                                                   })
                                                   .ToList();
            
            if (condition.Count == 0) {
                EditorGUI.LabelField(position, "No manual conditions found");
                return;
            }
            
            DrawGUID(guid);
            
            var previousSelectedIndex = condition.FindIndex(c => c.Guid == guid);
            _savedValue = previousSelectedIndex != -1 
                              ? condition[previousSelectedIndex].Combined 
                              : string.Empty;
            
            if (_playerTypedValue == null) {
                if (previousSelectedIndex != -1) {
                    _playerTypedValue = condition[previousSelectedIndex].FriendlyName;
                } else {
                    _playerTypedValue = string.Empty;
                    SetGUID(property, guid, default);
                }
            }
            
            // a search box that is also a preview of the selected condition
            EditorGUI.BeginChangeCheck();
            _playerTypedValue = EditorGUILayout.TextField("Entry: ", _playerTypedValue);
            if (EditorGUI.EndChangeCheck() && (previousSelectedIndex == -1 || _playerTypedValue != condition[previousSelectedIndex].FriendlyName)) {
                var newConditionIndex = condition.FindIndex(c => c.FriendlyName == _playerTypedValue);
                if (newConditionIndex != -1) {
                    SetGUID(property, guid, condition[newConditionIndex].Guid.GUID);
                }
            }

            // search result dropdown. no search -> show all options
            var filteredOptions = _playerTypedValue == string.Empty
                                           ? condition.ToArray() 
                                           : condition.OrderedSearchFilter(_playerTypedValue, static s => s.Combined).ToArray();
            previousSelectedIndex = filteredOptions.IndexOf(o => o.Combined == _savedValue);
            
            var popupSelectionIndex = EditorGUILayout.Popup(GUIContent.none, previousSelectedIndex, filteredOptions.Select(o => o.Combined).ToArray());
            if (popupSelectionIndex != previousSelectedIndex) {
                SetGUID(property, guid, filteredOptions[popupSelectionIndex].Guid.GUID);
                _playerTypedValue = filteredOptions[popupSelectionIndex].FriendlyName;
            }
        }

        static void SetGUID(SerializedProperty property, JournalGuid guid, Guid newGuid) {
            guid.EDITOR_SetGuid(newGuid);
            property.boxedValue = guid;
        }

        void HandleGUIDPreview(Rect position, SerializedProperty property, JournalGuid guid) {
            DrawUnlockedEntryInfo(position, guid);
            if (PrefabStageUtility.GetCurrentPrefabStage() == null) {
                DrawGUID(guid);
                return;
            }
            
            var savedGuid = guid.GUID;
            int uniquePathHash = UniquePathHash(property);
            if (savedGuid == Guid.Empty || !GUIDPairs.Contains(new GuidPathPair {guid = savedGuid, path = uniquePathHash})) {
                if (savedGuid == Guid.Empty || !Guids.Add(savedGuid)) {
                    var newGuid = Guid.NewGuid();
                    SetGUID(property, guid, newGuid);
                    Guids.Add(newGuid);
                }

                GUIDPairs.Add(new GuidPathPair {guid = guid.GUID, path = uniquePathHash});
            }

            DrawGUID(guid);
        }

        static void DrawGUID(JournalGuid guid) {
            if (JournalGuid.EDITOR_GuidsVisible) {
                var color = GUI.color;
                GUI.color = new Color(.7f, .7f, .7f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(guid.GUID.ToString(), EditorStyles.miniLabel);
                if (Application.isPlaying && GUILayout.Button("Unlock", GUILayout.Width(50))) {
                    World.Any<PlayerJournal>().UnlockEntry(guid.GUID, JournalSubTabType.Characters);
                }
                EditorGUILayout.EndHorizontal();
                GUI.color = color;
            }
        }

        void DrawUnlockedEntryInfo(Rect position, JournalGuid guid) {
            if (!Application.isPlaying) {
                return;
            }

            PlayerJournal playerJournal = World.Any<PlayerJournal>();
            if (playerJournal == null) {
                return;
            }
            
            if (playerJournal.WasEntryUnlocked(guid.GUID)) {
                var color = GUI.color;
                GUI.color = Color.green;
                EditorGUILayout.LabelField("Entry Unlocked", EditorStyles.miniLabel);
                GUI.color = color;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return 0;
        }

        static int UniquePathHash(SerializedProperty property) {
            string propertyPropertyPath = property.propertyPath;
            int result = 0;
            unchecked {
                result = (propertyPropertyPath.GetHashCode() * 397) ^ property.serializedObject.targetObject.GetInstanceID();
            }
            return result;
        }

        struct GuidPathPair : IEquatable<GuidPathPair> {
            public Guid guid;
            public int path;
            
            public override bool Equals(object obj) {
                return obj is GuidPathPair other && guid == other.guid && path == other.path;
            }

            public bool Equals(GuidPathPair other) {
                return guid == other.guid && path == other.path;
            }

            public override int GetHashCode() {
                unchecked {
                    return (guid.GetHashCode() * 397) ^ path;
                }
            }

            public static bool operator ==(GuidPathPair left, GuidPathPair right) {
                return left.Equals(right);
            }

            public static bool operator !=(GuidPathPair left, GuidPathPair right) {
                return !left.Equals(right);
            }
        }
    }
}