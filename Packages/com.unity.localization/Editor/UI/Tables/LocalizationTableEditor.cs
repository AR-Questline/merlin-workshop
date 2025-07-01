using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    [CustomEditor(typeof(LocalizationTable), true)]
    class LocalizationTableEditor : UnityEditor.Editor
    {
        LocalizationTable m_TargetTable;
        GUIContent m_TableEditorButton;
        GUIContent m_CollectionButton;
        SerializedProperty m_LocaleId;
        SerializedProperty m_SharedTableData;

        SerializedObject m_SharedTableDataSerializedObject;
        SerializedProperty m_TableCollectionName;

        LocalizationTableCollection m_Collection;
        LocalizationTableCollection m_SharedTableDataCollection;
        List<LocalizationTable> m_PossibleTableCollection = new List<LocalizationTable>(); // If m_Collection is null

        string m_MissingSharedDataEntriesWarning;

        class Styles
        {
            public static readonly GUIContent addToCollection = EditorGUIUtility.TrTextContent("Add Table to Collection", "Adds the table to the collection that shares the same Shared Table Data");
            public static readonly GUIContent createAssetTableCollection = EditorGUIUtility.TrTextContent("Create Asset Table Collection", "Creates a new asset table collection from the loose tables.");
            public static readonly GUIContent createStringTableCollection = EditorGUIUtility.TrTextContent("Create String Table Collection", "Creates a new string table collection from the loose tables.");
            public static readonly GUIContent missingEntriesAdd = EditorGUIUtility.TrTextContent("Add to Shared Data", "Add the entries to the Shared Table Data so that they can be used.");
            public static readonly GUIContent missingEntriesRemove = EditorGUIUtility.TrTextContent("Remove from Table", "Removed the entries from the table");
            public static readonly GUIContent removeFromCollection = EditorGUIUtility.TrTextContent("Remove Table from Collection", "Removes the table from the collection so that is is not used in the project");
            public static readonly GUIContent removeTableFromList = new GUIContent("-");
            public static readonly GUIContent sharedTableData = EditorGUIUtility.TrTextContent("Shared Table");
        }

        public virtual void OnEnable()
        {
            Undo.undoRedoPerformed += ValidateTableData;
            LocalizationEditorSettings.EditorEvents.TableAddedToCollection += OnCollectionTableChange;
            LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection += OnCollectionTableChange;
            LocalizationEditorSettings.EditorEvents.CollectionAdded += OnCollectionChange;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved += OnCollectionChange;

            if (target == null)
                return;

            m_TargetTable = target as LocalizationTable;
            m_LocaleId = serializedObject.FindProperty("m_LocaleId");
            m_SharedTableData = serializedObject.FindProperty("m_SharedData");
            m_TableEditorButton = EditorGUIUtility.TrTextContent("Open Table Editor", EditorGUIUtility.ObjectContent(target, target.GetType()).image);
            ValidateTableData();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= ValidateTableData;
            LocalizationEditorSettings.EditorEvents.TableAddedToCollection -= OnCollectionTableChange;
            LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection -= OnCollectionTableChange;
            LocalizationEditorSettings.EditorEvents.CollectionAdded -= OnCollectionChange;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved -= OnCollectionChange;
        }

        void ValidateTableData()
        {
            ResolveTableCollection();
            CheckForEntriesMissingFromSharedTableData(MissingEntryAction.Nothing);
        }

        void OnCollectionTableChange(LocalizationTableCollection col, LocalizationTable table) => ResolveTableCollection();
        void OnCollectionChange(LocalizationTableCollection col) => ResolveTableCollection();

        void ResolveTableCollection()
        {
            m_PossibleTableCollection.Clear();
            m_Collection = LocalizationEditorSettings.GetCollectionFromTable(m_TargetTable);

            if (m_TargetTable.SharedData == null)
                return;

            m_SharedTableDataSerializedObject = new SerializedObject(m_TargetTable.SharedData);
            m_TableCollectionName = m_SharedTableDataSerializedObject.FindProperty("m_TableCollectionName");

            if (m_Collection != null)
            {
                m_CollectionButton = EditorGUIUtility.TrTextContent("Select Collection", EditorGUIUtility.ObjectContent(m_Collection, m_Collection.GetType()).image);
                return;
            }

            m_SharedTableDataCollection = LocalizationEditorSettings.GetCollectionForSharedTableData(m_TargetTable.SharedData);
            if (m_SharedTableDataCollection != null)
                return;

            LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(m_TargetTable.SharedData, m_PossibleTableCollection);
        }

        void CheckForEntriesMissingFromSharedTableData(MissingEntryAction missingEntryAction)
        {
            if (m_TargetTable.SharedData == null)
                return;

            if (m_TargetTable is StringTable stringTable)
            {
                HandleMissingEntriesFromSharedTableData<StringTable, StringTableEntry>(stringTable, missingEntryAction);
            }
            else if (m_TargetTable is AssetTable assetTable)
            {
                HandleMissingEntriesFromSharedTableData<AssetTable, AssetTableEntry>(assetTable, missingEntryAction);
            }
        }

        void HandleMissingEntriesFromSharedTableData<T, TEntry>(T table, MissingEntryAction missingEntryAction)
            where T : DetailedLocalizationTable<TEntry>
            where TEntry : TableEntry
        {
            m_MissingSharedDataEntriesWarning = null;
            var missingEntries = table.CheckForMissingSharedTableDataEntries(missingEntryAction);

            if (missingEntryAction != MissingEntryAction.Nothing || missingEntries.Count == 0)
                return;

            using (StringBuilderPool.Get(out var sb))
            {
                sb.Append("This table contains some entries that are missing from Shared Table Data.\nThese entries will be ignored:");
                foreach (var entry in missingEntries)
                {
                    var value = string.IsNullOrEmpty(entry.LocalizedValue) ? "<empty>" : entry.LocalizedValue;
                    sb.Append($"\n{entry.KeyId} - {value}");
                }
                m_MissingSharedDataEntriesWarning = sb.ToString();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_SharedTableDataSerializedObject?.Update();

            EditorGUILayout.LabelField("Table Collection Name", m_TableCollectionName?.stringValue);
            EditorGUILayout.PropertyField(m_LocaleId);

            EditorGUILayout.Space();

            if (m_Collection == null)
            {
                DrawLooseTableGUI();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(m_CollectionButton, EditorStyles.miniButton, GUILayout.Width(150), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    Selection.activeObject = m_Collection;
                }
                if (GUILayout.Button(m_TableEditorButton, EditorStyles.miniButton, GUILayout.Width(150), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    LocalizationTablesWindow.ShowWindow(m_Collection);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button(Styles.removeFromCollection))
                {
                    m_Collection.RemoveTable(m_TargetTable, createUndo: true);
                }

                if (!string.IsNullOrEmpty(m_MissingSharedDataEntriesWarning))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox(m_MissingSharedDataEntriesWarning, MessageType.Warning);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(Styles.missingEntriesAdd))
                    {
                        Undo.RecordObject(m_TargetTable.SharedData, "Add Missing Entries");
                        CheckForEntriesMissingFromSharedTableData(MissingEntryAction.AddEntriesToSharedData);
                        EditorUtility.SetDirty(m_TargetTable.SharedData);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button(Styles.missingEntriesRemove))
                    {
                        Undo.RecordObject(m_TargetTable, "Remove Missing Entries");
                        CheckForEntriesMissingFromSharedTableData(MissingEntryAction.RemoveEntriesFromTable);
                        EditorUtility.SetDirty(m_TargetTable);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawLooseTableGUI()
        {
            if (m_TargetTable.SharedData == null)
            {
                EditorGUILayout.HelpBox("Shared Table Data is missing. Please add the missing asset or create a new one.", MessageType.Error);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.ObjectField(m_SharedTableData);
                if (EditorGUI.EndChangeCheck())
                {
                    ResolveTableCollection();
                }
                return;
            }

            EditorGUILayout.HelpBox("Table does not belong to a Collection and will be ignored.", MessageType.Warning);
            if (m_SharedTableDataCollection != null)
            {
                if (GUILayout.Button(Styles.addToCollection))
                {
                    m_SharedTableDataCollection.AddTable(m_TargetTable, createUndo: true);
                }
                return;
            }

            if (m_PossibleTableCollection.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The following loose tables could be combined into a new Table Collection", MessageType.Info);
                for (int i = 0; i < m_PossibleTableCollection.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(m_PossibleTableCollection[i].name, EditorStyles.label))
                    {
                        EditorGUIUtility.PingObject(m_PossibleTableCollection[i]);
                    }
                    if (GUILayout.Button(Styles.removeTableFromList, GUILayout.Width(40)))
                    {
                        m_PossibleTableCollection.RemoveAt(i);
                        EditorGUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (m_PossibleTableCollection.Count > 0)
            {
                var isStringTable = typeof(StringTable).IsAssignableFrom(m_PossibleTableCollection[0].GetType());
                var label = isStringTable ? Styles.createStringTableCollection : Styles.createAssetTableCollection;
                if (GUILayout.Button(label))
                {
                    var defaultDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_TargetTable.SharedData));
                    var path = EditorUtility.SaveFilePanel("Create Table Collection", defaultDirectory, m_TargetTable.TableCollectionName, "asset");
                    if (string.IsNullOrEmpty(path))
                        return;
                    LocalizationEditorSettings.CreateCollectionFromLooseTables(m_PossibleTableCollection, path);
                }
            }
        }
    }
}
