using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class MetadataEditorWindow : EditorWindow
    {
        VisualElement m_Contents;
        static MetadataEditorWindow s_Instance;

        LocalizationTableCollection m_LocalizationTableCollection;
        Locale m_Locale;

        LocalizationTable m_LocalizationTable;
        long m_TableEntryId;

        public static MetadataEditorWindow ShowWindow(LocalizationTableCollection collection, Locale locale)
        {
            s_Instance = GetWindow<MetadataEditorWindow>(true, "", true);
            s_Instance.EditTableMetadata(collection, locale);
            s_Instance.ShowUtility();
            return s_Instance;
        }

        public static MetadataEditorWindow ShowWindow(LocalizationTable table, long entryId)
        {
            s_Instance = GetWindow<MetadataEditorWindow>(true, "", true);
            s_Instance.EditTableEntryMetadata(table, entryId);
            s_Instance.ShowUtility();
            return s_Instance;
        }

        public static void CloseWindow()
        {
            if (s_Instance != null)
            {
                s_Instance.Close();
                s_Instance = null;
            }
        }

        public static bool IsSelectedForEditing(LocalizationTableCollection collection, Locale locale)
        {
            if (s_Instance == null)
                return false;
            return s_Instance.m_LocalizationTableCollection == collection && s_Instance.m_Locale == locale;
        }

        public static bool IsSelectedForEditing(LocalizationTable table, long entryId)
        {
            if (s_Instance == null)
                return false;
            return s_Instance.m_LocalizationTable == table && s_Instance.m_TableEntryId == entryId;
        }

        public static bool IsSelectedForEditing(long entryId)
        {
            if (s_Instance == null)
                return false;
            return s_Instance.m_TableEntryId == entryId;
        }

        void OnEnable()
        {
            var asset = Resources.GetTemplateAsset(nameof(MetadataEditorWindow));
            asset.CloneTree(rootVisualElement);

            m_Contents = rootVisualElement.Q("contents");
            var doneButton = rootVisualElement.Q<Button>();
            doneButton.clicked += Close;
        }

        void OnDestroy()
        {
            // Refresh the main window
            LocalizationTablesWindow.s_Instance?.Repaint();
        }

        void ResetContents()
        {
            m_Contents.Clear();
            m_LocalizationTableCollection = null;
            m_Locale = null;
            m_LocalizationTable = null;
            m_TableEntryId = 0;
        }

        void EditTableEntryMetadata(LocalizationTable table, long entryId)
        {
            ResetContents();

            m_LocalizationTable = table;
            m_TableEntryId = entryId;

            bool isStringTable = table is StringTable;

            var metadataLabel = new GUIContent("Metadata");

            // Shared data
            var sharedIndex = table.SharedData.Entries.FindIndex(e => e.Id == entryId);
            Debug.Assert(sharedIndex != -1, $"Could not find index of key {entryId}");
            var sharedSerializedObject = new SerializedObject(table.SharedData);

            var sharedSerializedEditor = new MetadataCollectionField() { Type = new MetadataTypeAttribute(isStringTable ? MetadataType.SharedStringTableEntry : MetadataType.SharedAssetTableEntry) };
            var sharedEntryProperty = sharedSerializedObject.FindProperty($"m_Entries.Array.data[{sharedIndex}].m_Metadata");
            var sharedLabel = new GUIContent("Shared Entry");
            var sharedEditor = new IMGUIContainer(() =>
            {
                sharedSerializedObject.Update();

                EditorGUILayout.LabelField(sharedLabel, EditorStyles.boldLabel);
                var rect = EditorGUILayout.GetControlRect(true, sharedSerializedEditor.GetPropertyHeight(sharedEntryProperty, metadataLabel));
                sharedSerializedEditor.OnGUI(rect, sharedEntryProperty, metadataLabel);
                sharedSerializedObject.ApplyModifiedProperties();
            });
            m_Contents.Add(sharedEditor);

            // Table data
            var tableIndex = table.TableData.FindIndex(d => d.Id == entryId);
            var tableSerializedObject = new SerializedObject(table);
            if (tableIndex == -1)
            {
                EditorUtility.SetDirty(table);
                table.CreateEmpty(entryId);
                tableSerializedObject.Update();

                tableIndex = table.TableData.FindIndex(d => d.Id == entryId);
                if (tableIndex == -1)
                    throw new System.Exception($"Failed to create or find a new entry for {entryId} in the table");
            }

            var tableEntryProperty = tableSerializedObject.FindProperty($"m_TableData.Array.data[{tableIndex}].m_Metadata");
            var tableSerializedEditor = new MetadataCollectionField() { Type = new MetadataTypeAttribute(isStringTable ? MetadataType.StringTableEntry : MetadataType.AssetTableEntry) };
            var tableLabel = new GUIContent($"{m_Locale?.ToString()} Entry Metadata");
            var tableEditor = new IMGUIContainer(() =>
            {
                tableSerializedObject.Update();

                EditorGUILayout.LabelField(tableLabel, EditorStyles.boldLabel);
                var rect = EditorGUILayout.GetControlRect(true, tableSerializedEditor.GetPropertyHeight(tableEntryProperty, metadataLabel));
                tableSerializedEditor.OnGUI(rect, tableEntryProperty, metadataLabel);
                tableSerializedObject.ApplyModifiedProperties();
            });
            m_Contents.Add(tableEditor);

            var entry = table.SharedData.GetEntry(entryId);
            var shortKey = entry.Key;
            if (shortKey.Length > 20)
                shortKey = shortKey.Substring(0, 20) + "...";

            var locale = LocalizationEditorSettings.GetLocale(table.LocaleIdentifier);
            titleContent = new GUIContent($"{shortKey} ({locale}) Entry metadata", isStringTable ? EditorIcons.StringTableCollection : EditorIcons.AssetTableCollection);
        }

        void EditTableMetadata(LocalizationTableCollection collection, Locale locale)
        {
            ResetContents();

            m_LocalizationTableCollection = collection;
            m_Locale = locale;

            bool isStringTable = collection is StringTableCollection;

            titleContent = new GUIContent(locale.ToString() + " metadata", isStringTable ? EditorIcons.StringTableCollection : EditorIcons.AssetTableCollection);

            var serializedObjectSharedData = new SerializedObject(collection.SharedData);
            var sharedTableDataEditor = new IMGUIContainer(() =>
            {
                serializedObjectSharedData.Update();
                EditorGUILayout.LabelField("Shared", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObjectSharedData.FindProperty("m_Metadata"));
                serializedObjectSharedData.ApplyModifiedProperties();
            });
            m_Contents.Add(sharedTableDataEditor);

            var tablePropDrawer = new MetadataCollectionField();
            tablePropDrawer.Type = new MetadataTypeAttribute(isStringTable ? MetadataType.StringTable : MetadataType.AssetTable);
            var label = new GUIContent("Metadata");

            var serializedObjectTable = new SerializedObject(collection.GetTable(locale.Identifier));
            var metadataLabel = new GUIContent(locale.ToString());
            var tableEditor = new IMGUIContainer(() =>
            {
                serializedObjectTable.Update();
                EditorGUILayout.LabelField(metadataLabel, EditorStyles.boldLabel);

                var p = serializedObjectTable.FindProperty("m_Metadata");
                var rect = EditorGUILayout.GetControlRect(true, tablePropDrawer.GetPropertyHeight(p, label));
                tablePropDrawer.OnGUI(rect, p, label);
                serializedObjectTable.ApplyModifiedProperties();
            });
            m_Contents.Add(tableEditor);
        }
    }
}
