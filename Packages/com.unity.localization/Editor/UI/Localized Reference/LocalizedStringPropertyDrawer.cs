using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;

#if ENABLE_SEARCH
using UnityEditor.Localization.Search;
#endif

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(LocalizedString), true)]
    class LocalizedStringPropertyDrawer : LocalizedReferencePropertyDrawer<StringTableCollection>
    {
        static GUIStyle s_FoldoutStyle;

        public class StringPropertyData : Data
        {
            List<LocaleField> m_SmartFormatFields;

            public LocalizedString localizedString;
            public ReorderableListExtended variableArguments;
            public string expandedSessionKey;

            public class LocaleField
            {
                bool m_Expanded;
                readonly string m_ExpandedSessionKey;

                public Locale Locale { get; set; }
                public SmartFormatFieldIMGUI SmartEditor { get; set; }

                public bool Expanded
                {
                    get => m_Expanded;
                    set
                    {
                        if (m_Expanded == value)
                            return;

                        m_Expanded = value;
                        SessionState.SetBool(m_ExpandedSessionKey, value);
                    }
                }

                public LocaleField(string expandedSessionKey)
                {
                    m_ExpandedSessionKey = expandedSessionKey;
                    m_Expanded = SessionState.GetBool(m_ExpandedSessionKey, false);
                }
            }

            public List<LocaleField> LocaleFields
            {
                get
                {
                    if (m_SmartFormatFields == null && SelectedTableEntry != null)
                    {
                        m_SmartFormatFields = new List<LocaleField>();
                        var projectLocales = LocalizationEditorSettings.GetLocales();

                        foreach (var locale in projectLocales)
                        {
                            var table = SelectedTableCollection.GetTable(locale.Identifier);
                            m_SmartFormatFields.Add(new LocaleField(expandedSessionKey + locale) { Locale = locale, SmartEditor = CreateSmartFormatFieldForTable(table) });
                        }
                    }
                    return m_SmartFormatFields;
                }
            }

            public StringPropertyData()
            {
                assetType = typeof(string);
            }

            public SmartFormatFieldIMGUI CreateSmartFormatFieldForTable(LocalizationTable table)
            {
                if (table is StringTable stringTable)
                {
                    var smartField = new SmartFormatFieldIMGUI();
                    smartField.Table = stringTable;
                    smartField.KeyId = SelectedTableEntry.Id;
                    smartField.RawText = SelectedTableEntry.Key;
                    smartField.ShowMetadataButton = false;
                    smartField.ShowPreviewTab = true;
                    smartField.MinHeight = EditorGUIUtility.singleLineHeight;
                    smartField.LocalizedString = localizedString;
                    smartField.RefreshData();
                    return smartField;
                }
                return null;
            }

            public override SharedTableData.SharedTableEntry SelectedTableEntry
            {
                set
                {
                    m_SmartFormatFields = null; // Reset cache.
                    base.SelectedTableEntry = value;
                }
            }

            public override void Reset()
            {
                base.Reset();
                m_SmartFormatFields = null;
            }

            public override void Init(SerializedProperty property)
            {
                base.Init(property);
                serializedObject = property.serializedObject;
                tableReference = new SerializedTableReference(property.FindPropertyRelative("m_TableReference"));
                tableEntryReference = new SerializedTableEntryReference(property.FindPropertyRelative("m_TableEntryReference"));

                if (LocaleFields != null)
                {
                    foreach (var field in LocaleFields)
                    {
                        field.SmartEditor?.RefreshData();
                    }
                }
            }
        }

        static LocalizedStringPropertyDrawer()
        {
            GetProjectTableCollections = LocalizationEditorSettings.GetStringTableCollections;
        }

        public override Data CreatePropertyData(SerializedProperty property)
        {
            var prop = new StringPropertyData()
            {
                entryNameLabel = Styles.entryName,
                expandedSessionKey = $"{property.serializedObject.targetObject.GetInstanceID()}-{property.propertyPath}",
                localizedString = property.GetActualObjectForSerializedProperty<LocalizedString>(fieldInfo),
                variableArguments = new ReorderableListExtended(property.serializedObject, property.FindPropertyRelative("m_LocalVariables"))
                {
                    Header = EditorGUIUtility.TrTextContent("Local Variables"),
                    onAddDropdownCallback = ShowArgumentsAddMenu,
                }
            };

            prop.variableArguments.drawElementCallback = (r, i, a, f) => DrawArgumentElement(prop.variableArguments, r, i, a, f);
            prop.variableArguments.elementHeightCallback = i => GetArgumentElementHeight(prop.variableArguments, i);
            return prop;
        }

        static bool HasPersistentVariablesSource() => LocalizationSettings.StringDatabase?.SmartFormatter?.GetSourceExtension<PersistentVariablesSource>() != null;

        void ShowArgumentsAddMenu(Rect rect, ReorderableList list)
        {
            var menu = new GenericMenu();
            TypeUtility.PopulateMenuWithCreateItems(menu, typeof(IVariable), type =>
            {
                var element = list.serializedProperty.AddArrayElement();

                var variable = element.FindPropertyRelative("variable");
                variable.managedReferenceValue = Activator.CreateInstance(type);

                var name = element.FindPropertyRelative("name");
                name.stringValue = list.serializedProperty.arraySize > 1 ? $"variable-{list.serializedProperty.arraySize - 1}" : "variable";

                list.serializedProperty.serializedObject.ApplyModifiedProperties();
            });

            menu.ShowAsContext();
        }

        void DrawArgumentElement(ReorderableListExtended list, Rect rect, int idx, bool isActive, bool isFocused)
        {
            rect.yMin += EditorGUIUtility.standardVerticalSpacing;

            var element = list.serializedProperty.GetArrayElementAtIndex(idx);
            var name = element.FindPropertyRelative("name");
            var variable = element.FindPropertyRelative("variable");
            var label = variable.hasMultipleDifferentValues ? Styles.mixedValueContent : ManagedReferenceUtility.GetDisplayName(variable.managedReferenceFullTypename);

            EditorGUI.BeginChangeCheck();
            var nameRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(nameRect, Styles.variableName, name);
            var newName = EditorGUI.TextField(nameRect, Styles.variableName, name.stringValue);
            EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                name.stringValue = newName.ReplaceWhiteSpaces("-");
            }

            rect.yMin = nameRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, variable, label, true);
        }

        float GetArgumentElementHeight(ReorderableListExtended list, int idx)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(idx);
            var variable = element.FindPropertyRelative("variable");
            return EditorGUI.GetPropertyHeight(variable, GUIContent.none, true) + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 3;
        }

        public override void OnGUI(Data data, Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(data, position, property, label);

            if (!property.isExpanded)
                return;

            var stringPropertyData = (StringPropertyData)data;
            var height = stringPropertyData.variableArguments.GetHeight();

            var listPosition = new Rect(position.x, position.yMax - height - EditorGUIUtility.standardVerticalSpacing, position.width, height);
            stringPropertyData.variableArguments.DoList(listPosition);

            if (stringPropertyData.variableArguments.count > 0 && LocalizationSettings.StringDatabase != null && !HasPersistentVariablesSource())
            {
                LocalizationSettings.StringDatabase.SmartFormatter.SourceExtensions.Insert(1, new PersistentVariablesSource(LocalizationSettings.StringDatabase.SmartFormatter));
                EditorUtility.SetDirty(LocalizationSettings.Instance);
                Debug.LogWarning("A PersistentVariablesSource is required to use Format Arguments. One has been added to the active LocalizationSettings.", property.serializedObject.targetObject);
            }
        }

        protected override void DrawTableEntryDetails(ref Rect rowPosition, Data data, Rect position)
        {
            base.DrawTableEntryDetails(ref rowPosition, data, position);
            var stringPropertyData = (StringPropertyData)data;

            // We want to clip long labels (case LOC-84)
            if (s_FoldoutStyle == null)
                s_FoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader) { clipping = TextClipping.Clip };

            for (int i = 0; i < stringPropertyData.LocaleFields.Count; ++i)
            {
                var field = stringPropertyData.LocaleFields[i];
                var label = new GUIContent(field.Locale.Identifier.ToString());

                // Is this a missing table?
                if (field.SmartEditor == null)
                {
                    rowPosition.height = EditorGUIUtility.singleLineHeight;
                    var buttonPosition = EditorGUI.PrefixLabel(rowPosition, label);
                    if (GUI.Button(buttonPosition, "Create Table"))
                    {
                        var table = stringPropertyData.SelectedTableCollection.AddNewTable(field.Locale.Identifier);
                        field.SmartEditor = stringPropertyData.CreateSmartFormatFieldForTable(table);
                        stringPropertyData.LocaleFields[i] = field;
                        GUIUtility.ExitGUI();
                    }
                    rowPosition.MoveToNextLine();
                    continue;
                }

                // Locale label/foldout
                rowPosition.height = EditorGUIUtility.singleLineHeight;

                float xMin = rowPosition.xMin; // Store the x position so we can restore it at the end.
                rowPosition = EditorGUI.PrefixLabel(rowPosition, GUIContent.none);

                var labelWidth = EditorGUIUtility.labelWidth - ((EditorGUI.indentLevel + 1) * 15);
                var foldoutPos = new Rect(rowPosition.x, rowPosition.y, labelWidth, rowPosition.height);
                var labelPos = new Rect(rowPosition.x + labelWidth, rowPosition.y, rowPosition.width - labelWidth, rowPosition.height);
                field.Expanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutPos, field.Expanded, label, s_FoldoutStyle);

                // Preview label
                EditorGUI.LabelField(labelPos, field.SmartEditor.Label);
                rowPosition.MoveToNextLine();

                if (field.Expanded)
                {
                    rowPosition.height = field.SmartEditor.Height;
                    field.SmartEditor.Draw(rowPosition);
                    rowPosition.MoveToNextLine();
                }
                EditorGUI.EndFoldoutHeaderGroup();
                stringPropertyData.LocaleFields[i] = field;
                rowPosition.xMin = xMin;
            }
        }

        #if ENABLE_SEARCH
        protected override void ShowPicker(Data data, Rect dropDownPosition)
        {
            if (!LocalizationEditorSettings.UseLocalizedStringSearchPicker)
            {
                base.ShowPicker(data, dropDownPosition);
                return;
            }

            var provider = new StringTableSearchProvider();
            var context = UnityEditor.Search.SearchService.CreateContext(provider, FilterIds.StringTableProviderFilter);
            var picker = new LocalizedReferencePicker<StringTableCollection>(context, "string table entry", data.tableReference.Property, data.tableEntryReference.Property);
            picker.Show();
        }
        #endif

        public override float GetPropertyHeight(Data data, SerializedProperty property, GUIContent label)
        {
            float height = base.GetPropertyHeight(data, property, label);

            if (property.isExpanded)
            {
                var stringPropertyData = (StringPropertyData)data;

                height += stringPropertyData.variableArguments.GetHeight() + EditorGUIUtility.standardVerticalSpacing;

                if (data.SelectedTableEntry != null)
                {
                    foreach (var field in stringPropertyData.LocaleFields)
                    {
                        height += EditorStyles.foldoutHeader.fixedHeight + EditorGUIUtility.standardVerticalSpacing; // Locale label/foldout
                        if (field.Expanded && field.SmartEditor != null)
                        {
                            height += field.SmartEditor.Height + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                }
            }
            return height;
        }
    }
}
