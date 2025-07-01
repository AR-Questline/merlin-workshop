using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEditor.Localization.Reporting;
using UnityEditor.Localization.UI;
using UnityEngine;

namespace UnityEditor.Localization.Plugins.CSV
{
    class CsvExtensionPropertyDrawerData : PropertyDrawerExtendedData
    {
        public SerializedProperty m_Columns;
        public SerializedProperty m_ConnectedFile;
        public ReorderableListExtended m_ColumnsList;
    }

    [CustomPropertyDrawer(typeof(CsvExtension))]
    class CsvExtensionPropertyDrawer : PropertyDrawerExtended<CsvExtensionPropertyDrawerData>
    {
        class Styles
        {
            public static readonly GUIContent addDefaultColumns = EditorGUIUtility.TrTextContent("Add Default Columns");
            public static readonly GUIContent addDefaultColumnsWithComments = EditorGUIUtility.TrTextContent("Add Default Columns(With Comments)");
            public static readonly GUIContent header = EditorGUIUtility.TrTextContent("Comma Separated Values (CSV)", EditorIcons.Csv);
            public static readonly GUIContent save = EditorGUIUtility.TrTextContent("Save...");
            public static readonly GUIContent open = EditorGUIUtility.TrTextContent("Open...");
            public static readonly GUIContent openMerge = EditorGUIUtility.TrTextContent("Open(Merge)...");
            public static readonly GUIContent import = EditorGUIUtility.TrTextContent("Import");
            public static readonly GUIContent importMerge = EditorGUIUtility.TrTextContent("Import(Merge)");
            public static readonly GUIContent export = EditorGUIUtility.TrTextContent("Export");
            public static readonly GUIContent show = EditorGUIUtility.TrTextContent("Show Folder");
        }

        public override CsvExtensionPropertyDrawerData CreatePropertyData(SerializedProperty property)
        {
            var data = new CsvExtensionPropertyDrawerData
            {
                m_Columns = property.FindPropertyRelative("m_ColumnsList"),
                m_ConnectedFile = property.FindPropertyRelative("m_ConnectedFile")
            };
            data.m_ColumnsList = new ReorderableListExtended(property.serializedObject, data.m_Columns);
            data.m_ColumnsList.AddMenuType = typeof(CsvColumns);
            data.m_ColumnsList.AddMenuItems = menu =>
            {
                menu.AddItem(Styles.addDefaultColumns, false, () => AddDefaultColumns(data, false));
                menu.AddItem(Styles.addDefaultColumnsWithComments, false, () => AddDefaultColumns(data, true));
            };

            return data;
        }

        static void AddDefaultColumns(CsvExtensionPropertyDrawerData data, bool includeComments)
        {
            var columns = ColumnMapping.CreateDefaultMapping(includeComments);

            data.m_Columns.ClearArray();
            foreach (var c in columns)
            {
                var colElement = data.m_Columns.AddArrayElement();
                colElement.managedReferenceValue = c;
            }
            data.m_Columns.serializedObject.ApplyModifiedProperties();
        }

        internal static void Export(string path, StringTableCollection collection, IList<CsvColumns> columns)
        {
            // Use FileShare.ReadWrite to avoid IOException: Sharing violation (LOC-348)
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var stream = new StreamWriter(fs, new UTF8Encoding(false));
                var reporter = TaskReporter.CreateDefaultReporter();
                reporter.Start("Exporting " + path, string.Empty);
                Csv.Export(stream, collection, columns, reporter);
            }

            EditorUtility.RevealInFinder(path);
        }

        static void Import(string path, StringTableCollection collection, IList<CsvColumns> columns, bool merge)
        {
            // Use FileShare.ReadWrite to avoid IOException: Sharing violation (LOC-348)
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var stream = new StreamReader(fs);
                var reporter = TaskReporter.CreateDefaultReporter();
                reporter.Start("Importing " + path, string.Empty);
                Csv.ImportInto(stream, collection, columns, true, reporter, merge);
            }
        }

        public override float GetPropertyHeight(CsvExtensionPropertyDrawerData data, SerializedProperty property, GUIContent label)
        {
            float height = data.m_ColumnsList.GetHeight() + (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
            if (!string.IsNullOrEmpty(data.m_ConnectedFile.stringValue))
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
            return height;
        }

        public override void OnGUI(CsvExtensionPropertyDrawerData data, Rect position, SerializedProperty property, GUIContent label)
        {
            position.yMin += EditorGUIUtility.standardVerticalSpacing;
            position.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(position, Styles.header, EditorStyles.boldLabel);
            position.MoveToNextLine();

            position.height = data.m_ColumnsList.GetHeight();
            data.m_ColumnsList.DoList(position);
            position.MoveToNextLine();
            position.height = EditorGUIUtility.singleLineHeight;
            var buttonsRect = position.SplitIntoThreeParts();
            position.MoveToNextLine();

            if (GUI.Button(buttonsRect.left, Styles.save))
            {
                var target = property.GetActualObjectForSerializedProperty<CsvExtension>(fieldInfo);
                var collection = target.TargetCollection as StringTableCollection;

                var path = EditorUtility.SaveFilePanel("Export to CSV", MenuItems.PreviousDirectory, collection.TableCollectionName, "csv");
                path = PathHelper.MakePathRelative(path);
                if (!string.IsNullOrEmpty(path))
                {
                    data.m_ConnectedFile.stringValue = path;
                    Export(path, collection, target.Columns);
                    MenuItems.PreviousDirectory = path;
                }

                // We need to apply the changes here as we exit early (LOC-751).
                data.m_ConnectedFile.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }

            if (GUI.Button(buttonsRect.center, Styles.open))
            {
                var target = property.GetActualObjectForSerializedProperty<CsvExtension>(fieldInfo);
                var collection = target.TargetCollection as StringTableCollection;

                var path = EditorUtility.OpenFilePanel("Import CSV", MenuItems.PreviousDirectory, "csv");
                path = PathHelper.MakePathRelative(path);
                if (!string.IsNullOrEmpty(path))
                {
                    data.m_ConnectedFile.stringValue = path;
                    Import(path, collection, target.Columns, true);
                    MenuItems.PreviousDirectory = path;
                }

                // We need to apply the changes here as we exit early (LOC-751).
                data.m_ConnectedFile.serializedObject.ApplyModifiedProperties();

                GUIUtility.ExitGUI();
            }

            if (GUI.Button(buttonsRect.right, Styles.openMerge))
            {
                var target = property.GetActualObjectForSerializedProperty<CsvExtension>(fieldInfo);
                var collection = target.TargetCollection as StringTableCollection;

                var path = EditorUtility.OpenFilePanel("Import CSV", MenuItems.PreviousDirectory, "csv");
                path = PathHelper.MakePathRelative(path);
                if (!string.IsNullOrEmpty(path))
                {
                    data.m_ConnectedFile.stringValue = path;
                    Import(path, collection, target.Columns, false);
                    MenuItems.PreviousDirectory = path;
                }

                // We need to apply the changes here as we exit early (LOC-751).
                data.m_ConnectedFile.serializedObject.ApplyModifiedProperties();

                GUIUtility.ExitGUI();
            }

            if (!string.IsNullOrEmpty(data.m_ConnectedFile.stringValue))
            {
                position.height = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                EditorGUI.HelpBox(position, string.Empty, MessageType.None);

                position.height = EditorGUIUtility.singleLineHeight;
                var split = position.SplitHorizontalFixedWidthRight(20);

                EditorGUI.LabelField(split.left, data.m_ConnectedFile.stringValue, EditorStyles.miniLabel);
                if (GUI.Button(split.right, "-"))
                {
                    data.m_ConnectedFile.stringValue = string.Empty;
                }
                position.MoveToNextLine();

                var splitHalves = position.SplitHorizontal();
                var syncLeft = splitHalves.left.SplitHorizontal();
                var syncRight = splitHalves.right.SplitHorizontal();

                if (GUI.Button(syncLeft.left, Styles.export))
                {
                    var target = property.GetActualObjectForSerializedProperty<CsvExtension>(fieldInfo);
                    var collection = target.TargetCollection as StringTableCollection;
                    Export(data.m_ConnectedFile.stringValue, collection, target.Columns);
                }

                if (GUI.Button(syncLeft.right, Styles.show))
                {
                    EditorUtility.RevealInFinder(data.m_ConnectedFile.stringValue);
                }

                using (new EditorGUI.DisabledScope(!File.Exists(data.m_ConnectedFile.stringValue)))
                {
                    if (GUI.Button(syncRight.left, Styles.import))
                    {
                        var target = property.GetActualObjectForSerializedProperty<CsvExtension>(fieldInfo);
                        var collection = target.TargetCollection as StringTableCollection;
                        Import(data.m_ConnectedFile.stringValue, collection, target.Columns, true);
                    }

                    if (GUI.Button(syncRight.right, Styles.importMerge))
                    {
                        var target = property.GetActualObjectForSerializedProperty<CsvExtension>(fieldInfo);
                        var collection = target.TargetCollection as StringTableCollection;
                        Import(data.m_ConnectedFile.stringValue, collection, target.Columns, false);
                    }
                }
                position.MoveToNextLine();
            }
        }
    }
}
