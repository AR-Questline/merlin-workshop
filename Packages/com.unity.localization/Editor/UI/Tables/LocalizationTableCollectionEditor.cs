using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    [CustomEditor(typeof(LocalizationTableCollection), true)]
    class LocalizationTableCollectionEditor : UnityEditor.Editor
    {
        class Styles
        {
            public static readonly GUIContent addTable = EditorGUIUtility.TrTextContent("Add", "Add the table to the collection.");
            public static readonly GUIContent createTable = EditorGUIUtility.TrTextContent("Create", "Create a table for the Locale.");
            public static readonly GUIContent editCollection = EditorGUIUtility.TrTextContent("Open in Table Editor", "Open the collection for editing in the tables window.");
            public static readonly GUIContent extensions = EditorGUIUtility.TrTextContent("Extensions");
            public static readonly GUIContent group = EditorGUIUtility.TrTextContent("Group", "The Group is used to group together collections when displaying them in a menu, such as the Localization Tables Selected Table Collection field.");
            public static readonly GUIContent looseTables = EditorGUIUtility.TrTextContent("Loose Tables");
            public static readonly GUIContent looseTablesInfo = EditorGUIUtility.TrTextContent("The following tables do not belong to any collection and share the same Shared Table Data as this collection. They can be added to this collection.");
            public static readonly string missingSharedTableData = L10n.Tr("This collection is missing its Shared Table Data.");
            public static readonly GUIContent missingTables = EditorGUIUtility.TrTextContent("Missing Tables");
            public static readonly GUIContent missingTablesInfo = EditorGUIUtility.TrTextContent("These are tables that are missing for the Locales in the project.");
            public static readonly GUIContent noExtensions = EditorGUIUtility.TrTextContent("No Available Extensions");
            public static readonly GUIContent removeTable = EditorGUIUtility.TrTextContent("Remove", "Remove the table from the collection");
            public static readonly GUIContent tables = EditorGUIUtility.TrTextContent("Tables");
        }

        LocalizationTableCollection m_Collection;
        SerializedProperty m_Tables;
        SerializedProperty m_SharedTableData;
        SerializedProperty m_Group;
        SerializedProperty m_Extensions;
        internal List<LocalizationTable> m_LooseTables = new List<LocalizationTable>();
        internal List<(Locale locale, bool tableExists)> m_MissingTables = new List<(Locale, bool)>();
        ReorderableListExtended m_ExtensionsList;
        bool m_ShowLooseTables = true;
        bool m_ShowMissingTables = true;

        void OnEnable()
        {
            m_Collection = target as LocalizationTableCollection;
            m_Tables = serializedObject.FindProperty("m_Tables");
            m_SharedTableData = serializedObject.FindProperty("m_SharedTableData");
            m_Group = serializedObject.FindProperty("m_Group");
            m_Extensions = serializedObject.FindProperty("m_Extensions");

            m_ExtensionsList = new ReorderableListExtended(serializedObject, m_Extensions);
            m_ExtensionsList.AddMenuType = typeof(CollectionExtension);
            m_ExtensionsList.RequiredAttribute = target is StringTableCollection ? typeof(StringTableCollectionExtensionAttribute) : typeof(AssetTableCollectionExtensionAttribute);
            m_ExtensionsList.Header = Styles.extensions;
            m_ExtensionsList.NoItemMenuItem = Styles.noExtensions;
            m_ExtensionsList.CreateNewInstance = type =>
            {
                var instance = Activator.CreateInstance(type) as CollectionExtension;
                ((LocalizationTableCollection)target).AddExtension(instance);
                return instance;
            };
            m_ExtensionsList.onRemoveCallback += list =>
            {
                var collection = m_Extensions.GetArrayElementAtIndex(list.index).serializedObject.targetObject as LocalizationTableCollection;
                var extension = collection.Extensions[list.index];
                collection.RemoveExtension(extension);
            };

            LocalizationEditorSettings.EditorEvents.TableAddedToCollection += OnTableModified;
            LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection += OnTableModified;
            Undo.undoRedoPerformed += RefreshTables;
            RefreshTables();
        }

        void OnDisable()
        {
            LocalizationEditorSettings.EditorEvents.TableAddedToCollection -= OnTableModified;
            LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection -= OnTableModified;
            Undo.undoRedoPerformed -= RefreshTables;
        }

        void OnTableModified(LocalizationTableCollection col, LocalizationTable tbl)
        {
            if (col == m_Collection)
                RefreshTables();
        }

        void RefreshTables()
        {
            // Find loose tables
            m_LooseTables.Clear();

            if (m_Collection.SharedData == null)
                return;

            LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(m_Collection.SharedData, m_LooseTables);

            // Find missing tables by project locales
            var projectLocales = LocalizationEditorSettings.GetLocales();
            m_MissingTables.Clear();
            foreach (var locale in projectLocales)
            {
                if (!m_Collection.ContainsTable(locale.Identifier))
                {
                    var tableExists = m_LooseTables.Any(t => ReferenceEquals(m_Collection.SharedData, t.SharedData) && t.LocaleIdentifier == locale.Identifier);
                    m_MissingTables.Add((locale, tableExists));
                }
            }

            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (m_Collection.SharedData == null)
            {
                EditorGUILayout.HelpBox(Styles.missingSharedTableData, MessageType.Error);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SharedTableData);
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshTables();
                }
                return;
            }

            m_Tables.isExpanded = EditorGUILayout.Foldout(m_Tables.isExpanded, Styles.tables, true);
            if (m_Tables.isExpanded)
            {
                EditorGUI.indentLevel++;
                var tables = m_Collection.Tables;
                for (int i = 0; i < tables.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button(tables[i].asset?.name, EditorStyles.label))
                    {
                        EditorGUIUtility.PingObject(tables[i].asset);
                    }

                    if (GUILayout.Button(Styles.removeTable, GUILayout.Width(60)))
                    {
                        m_Collection.RemoveTable(tables[i].asset, createUndo: true);
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            // Loose tables
            if (m_LooseTables.Count > 0)
            {
                m_ShowLooseTables = EditorGUILayout.Foldout(m_ShowLooseTables, Styles.looseTables);
                if (m_ShowLooseTables)
                {
                    EditorGUILayout.HelpBox(Styles.looseTablesInfo);
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < m_LooseTables.Count; ++i)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(m_LooseTables[i].name, EditorStyles.label))
                        {
                            EditorGUIUtility.PingObject(m_LooseTables[i]);
                        }

                        if (GUILayout.Button(Styles.addTable, GUILayout.Width(50)))
                        {
                            m_Collection.AddTable(m_LooseTables[i], createUndo: true);
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // Missing tables
            if (m_MissingTables.Count > 0)
            {
                m_ShowMissingTables = EditorGUILayout.Foldout(m_ShowMissingTables, Styles.missingTables);
                if (m_ShowMissingTables)
                {
                    EditorGUILayout.HelpBox(Styles.missingTablesInfo);
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < m_MissingTables.Count; ++i)
                    {
                        var (locale, tableExists) = m_MissingTables[i];

                        EditorGUILayout.BeginHorizontal();
                        using (new EditorGUI.DisabledScope(tableExists))
                        {
                            if (GUILayout.Button(locale.name, EditorStyles.label))
                            {
                                EditorGUIUtility.PingObject(locale);
                            }

                            if (GUILayout.Button(Styles.createTable, GUILayout.Width(60)))
                            {
                                m_Collection.AddNewTable(locale.Identifier);
                                GUIUtility.ExitGUI();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
            }

            if (GUILayout.Button(Styles.editCollection))
            {
                LocalizationTablesWindow.ShowWindow(target as LocalizationTableCollection);
            }

            EditorGUILayout.PropertyField(m_Group, Styles.group);

            m_ExtensionsList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
