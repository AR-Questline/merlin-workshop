using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;

namespace UnityEditor.Localization.UI
{
    class PlatformOverridePropertyData : PropertyDrawerExtendedData
    {
        public SerializedProperty m_PlatformOverridesProperty;
        public ReorderableList m_PlatformOverridesList;
    }

    [CustomPropertyDrawer(typeof(PlatformOverride))]
    class PlatformOverridePropertyDrawer : PropertyDrawerExtended<PlatformOverridePropertyData>
    {
        class Styles
        {
            public static readonly GUIContent entry = EditorGUIUtility.TrTextContent("Table Entry");
            public static readonly GUIContent none = EditorGUIUtility.TrTextContent("None");
            public static readonly GUIContent reference = EditorGUIUtility.TrTextContent("Reference");
            public static readonly GUIContent tableCollection = EditorGUIUtility.TrTextContent("Table Collection");
        }

        LocalizationTableCollection m_Collection;
        Type m_TableType;

        static readonly GUIContent kPlatformOverrides = EditorGUIUtility.TrTextContent("Platform Overrides");
        static readonly int[] s_PlatformValues;

        static PlatformOverridePropertyDrawer()
        {
            s_PlatformValues = Enum.GetValues(typeof(RuntimePlatform)) as int[];
        }

        public override PlatformOverridePropertyData CreatePropertyData(SerializedProperty property)
        {
            var data = new PlatformOverridePropertyData
            {
                m_PlatformOverridesProperty = property.FindPropertyRelative("m_PlatformOverrides"),
            };

            data.m_PlatformOverridesList = new ReorderableList(property.serializedObject, data.m_PlatformOverridesProperty);
            data.m_PlatformOverridesList.drawElementCallback = (rect, index, isActive, isFocused) => DrawListElement(rect, index, isActive, isFocused, data);
            data.m_PlatformOverridesList.drawHeaderCallback = DrawListHeader;
            data.m_PlatformOverridesList.onAddDropdownCallback = (rect, _) => DrawAddDropdown(rect, data);
            data.m_PlatformOverridesList.elementHeightCallback = (index) => GetHeight(index, data);

            var target = data.m_PlatformOverridesList.serializedProperty.serializedObject.targetObject;
            SharedTableData sharedTableData = target as SharedTableData ?? (target as LocalizationTable)?.SharedData;
            Debug.Assert(sharedTableData != null, "Shared Table Data is null. Platform Override can only be used on a Shared Table Entry.");
            m_Collection = LocalizationEditorSettings.GetCollectionForSharedTableData(sharedTableData);
            m_TableType = m_Collection.GetType() == typeof(StringTableCollection) ? typeof(StringTable) : typeof(AssetTable);

            return data;
        }

        void DrawListElement(Rect rect, int index, bool isActive, bool isFocused, PlatformOverridePropertyData data)
        {
            rect.height = EditorGUIUtility.singleLineHeight;

            rect.xMin += 12; // Small indent so the foldout arrow does not cover the dragger.
            var element = data.m_PlatformOverridesList.serializedProperty.GetArrayElementAtIndex(index);
            var platform = (RuntimePlatform)element.FindPropertyRelative("platform").intValue;

            EditorGUI.LabelField(rect, EditorGUIUtility.TrTempContent(platform.ToString()), EditorStyles.boldLabel);
            rect.MoveToNextLine();

            var overrideTypeProperty = element.FindPropertyRelative("entryOverrideType");
            EditorGUI.PropertyField(rect, overrideTypeProperty);
            rect.MoveToNextLine();

            var overrideType = (EntryOverrideType)overrideTypeProperty.intValue;
            switch (overrideType)
            {
                case EntryOverrideType.Table:
                    DoTableGUI(rect, element);
                    break;
                case EntryOverrideType.Entry:
                    DoEntryGUI(rect, element);
                    break;
                case EntryOverrideType.TableAndEntry:
                    DoTableAndEntryGUI(rect, element);
                    break;
            }
        }

        static string GetTableLabel(TableReference tableReference)
        {
            if (tableReference.ReferenceType == TableReference.Type.Guid)
            {
                var guid = TableReference.StringFromGuid(tableReference.TableCollectionNameGuid);
                var collection = LocalizationEditorSettings.Instance.TableCollectionCache.FindCollectionFromDependencyGuid(guid);
                if (collection != null)
                    return collection.TableCollectionName;
                else
                    return "Missing Collection: " + guid;
            }
            else if (tableReference.ReferenceType == TableReference.Type.Name && !string.IsNullOrEmpty(tableReference.TableCollectionName))
            {
                return tableReference.TableCollectionName;
            }
            return "None";
        }

        static string GetEntryLabel(TableEntryReference entryReference, SharedTableData sharedTableData)
        {
            return entryReference.ReferenceType != TableEntryReference.Type.Empty ? entryReference.ResolveKeyName(sharedTableData) : "None";
        }

        void DoTableGUI(Rect rect, SerializedProperty property)
        {
            var tableRef = new SerializedTableReference(property.FindPropertyRelative("tableReference"));
            var valueLabel = new GUIContent(GetTableLabel(tableRef.Reference));

            var dropDownPosition = EditorGUI.PrefixLabel(rect, Styles.tableCollection);
            if (EditorGUI.DropdownButton(dropDownPosition, valueLabel, FocusType.Passive))
            {
                var treeSelection = new TableTreeView(m_TableType, sel =>
                {
                    tableRef.SetReference(sel);
                    var entryRef = new SerializedTableEntryReference(property.FindPropertyRelative("tableEntryReference"));
                    entryRef.SetReference(null);
                    property.serializedObject.ApplyModifiedProperties();
                });
                PopupWindow.Show(dropDownPosition, new TreeViewPopupWindow(treeSelection) { Width = dropDownPosition.width });
            }
        }

        void DoEntryGUI(Rect rect, SerializedProperty property)
        {
            var entryRef = new SerializedTableEntryReference(property.FindPropertyRelative("tableEntryReference"));
            var valueLabel = new GUIContent(GetEntryLabel(entryRef.Reference, m_Collection.SharedData));

            var dropDownPosition = EditorGUI.PrefixLabel(rect, Styles.entry);
            if (EditorGUI.DropdownButton(dropDownPosition, valueLabel, FocusType.Passive))
            {
                Type assetType;
                if (m_TableType == typeof(AssetTable))
                {
                    var assetTableCollection = m_Collection as AssetTableCollection;
                    assetType = assetTableCollection.GetEntryAssetType(entryRef.Reference);
                }
                else
                {
                    assetType = typeof(string);
                }

                var treeSelection = new EntryTreeView(assetType, m_Collection, (c, e) =>
                {
                    entryRef.SetReference(e);
                    var tableRef = new SerializedTableReference(property.FindPropertyRelative("tableReference"));
                    tableRef.SetReference(c);
                    property.serializedObject.ApplyModifiedProperties();
                });
                PopupWindow.Show(dropDownPosition, new TreeViewPopupWindow(treeSelection) { Width = dropDownPosition.width });
            }
        }

        void DoTableAndEntryGUI(Rect rect, SerializedProperty property)
        {
            var tableRef = new SerializedTableReference(property.FindPropertyRelative("tableReference"));
            var entryRef = new SerializedTableEntryReference(property.FindPropertyRelative("tableEntryReference"));
            GUIContent valueLabel;
            if (tableRef.Reference.ReferenceType != TableReference.Type.Empty && entryRef.Reference.ReferenceType != TableEntryReference.Type.Empty)
            {
                LocalizationTableCollection collection = null;
                if (m_TableType == typeof(StringTable))
                    collection = LocalizationEditorSettings.GetStringTableCollection(tableRef.Reference);
                else
                    collection = LocalizationEditorSettings.GetAssetTableCollection(tableRef.Reference);
                valueLabel = new GUIContent($"{GetTableLabel(tableRef.Reference)}/{GetEntryLabel(entryRef.Reference, collection?.SharedData)}");
            }
            else
            {
                valueLabel = Styles.none;
            }

            var dropDownPosition = EditorGUI.PrefixLabel(rect, Styles.reference);
            if (EditorGUI.DropdownButton(dropDownPosition, valueLabel, FocusType.Passive))
            {
                Type assetType;
                if (m_TableType == typeof(AssetTable))
                {
                    var assetTableCollection = m_Collection as AssetTableCollection;
                    assetType = assetTableCollection.GetEntryAssetType(entryRef.Reference);
                }
                else
                {
                    assetType = typeof(string);
                }

                var treeSelection = new TableEntryTreeView(assetType, (c, e) =>
                {
                    entryRef.SetReference(e);
                    tableRef.SetReference(c);
                    property.serializedObject.ApplyModifiedProperties();
                });
                PopupWindow.Show(dropDownPosition, new TreeViewPopupWindow(treeSelection) { Width = dropDownPosition.width });
            }
        }

        void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, kPlatformOverrides);
        }

        void DrawAddDropdown(Rect buttonRect, PlatformOverridePropertyData data)
        {
            using (HashSetPool<int>.Get(out var hashSet))
            {
                for (int i = 0; i < data.m_PlatformOverridesList.serializedProperty.arraySize; ++i)
                {
                    hashSet.Add(data.m_PlatformOverridesList.serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative("platform").intValue);
                }

                var menu = new GenericMenu();
                foreach (var platform in s_PlatformValues)
                {
                    if (!hashSet.Contains(platform))
                    {
                        var label = EditorGUIUtility.TrTextContent(((RuntimePlatform)platform).ToString());
                        menu.AddItem(label, false, () =>
                        {
                            AddPlatform(platform, data);
                        });
                    }
                }
                menu.DropDown(buttonRect);
            }
        }

        float GetHeight(int index, PlatformOverridePropertyData data)
        {
            var element = data.m_PlatformOverridesList.serializedProperty.GetArrayElementAtIndex(index);
            var overrideTypeProperty = element.FindPropertyRelative("entryOverrideType");
            var overrideType = (EntryOverrideType)overrideTypeProperty.intValue;

            float singleLine = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return overrideType == EntryOverrideType.None ? singleLine * 2 : singleLine * 3;
        }

        void AddPlatform(int platform, PlatformOverridePropertyData data)
        {
            var item = data.m_PlatformOverridesList.serializedProperty.AddArrayElement();
            item.FindPropertyRelative("platform").intValue = (int)platform;
            data.m_PlatformOverridesList.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        public override void OnGUI(PlatformOverridePropertyData data, Rect position, SerializedProperty property, GUIContent label)
        {
            data.m_PlatformOverridesList.DoList(position);
        }

        public override float GetPropertyHeight(PlatformOverridePropertyData data, SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            height += data.m_PlatformOverridesList.GetHeight();
            return height;
        }
    }
}
