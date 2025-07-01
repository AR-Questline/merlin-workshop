using System;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    abstract class LocalizedReferencePropertyDrawer<TCollection> : PropertyDrawerExtended<LocalizedReferencePropertyDrawer<TCollection>.Data>, IDisposable where TCollection : LocalizationTableCollection
    {
        protected static class Styles
        {
            public static readonly GUIContent addLocale = EditorGUIUtility.TrTextContent("Add Locale", "Add a new Locale.");
            public static readonly GUIContent addTable = EditorGUIUtility.TrTextContent("Create Table", "Create a new table for the selected locale.");
            public static readonly GUIContent addTableCollection = EditorGUIUtility.TrTextContent("Create Table Collection", "Create a new table collection for every Locale in the project");
            public static readonly GUIContent addTableEntry = EditorGUIUtility.TrTextContent("Add Table Entry", "Create a new table entry in the selected table collection.");
            public static readonly GUIContent defaultArg = EditorGUIUtility.TrTextContent("Default Argument", "The Default Argument will be the argument passed in at index 0 and will be used when no index placeholder is used");
            public static readonly GUIContent entryName = EditorGUIUtility.TrTextContent("Entry Name", "The name or key of the selected table entry");
            public static readonly GUIContent useFallback = EditorGUIUtility.TrTextContent("Enable Fallback", "Determines if a Fallback should be used when no value could be found for the Locale");
            public static readonly GUIContent noTableSelected = new GUIContent($"None({typeof(TCollection).Name})");
            public static readonly GUIContent mixedValueContent = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
            public static readonly GUIContent previewArguments = EditorGUIUtility.TrTextContent("Preview Arguments", "Arguments to pass to the string formatter. These are for preview purposes only and are not stored.");
            public static readonly GUIContent selectedTable = EditorGUIUtility.TrTextContent("Table Collection");
            public static readonly GUIContent variableName = EditorGUIUtility.TrTextContent("Variable Name");
            public static readonly GUIContent waitForCompletion = EditorGUIUtility.TrTextContent("Wait For Completion", "Should the operation wait for the localization operation to complete before returning, blocking the main thread, or allow it to finish asyncronously? Please note that this is not supported on WebGL.");
        }

        protected static Func<ReadOnlyCollection<TCollection>> GetProjectTableCollections { get; set; }

        public class Data : PropertyDrawerExtendedData
        {
            public SerializedProperty rootProperty;
            public SerializedObject serializedObject;
            public SerializedTableReference tableReference;
            public SerializedTableEntryReference tableEntryReference;
            public SerializedProperty fallbackState;
            public SerializedProperty waitForCompletion;
            public Type assetType;
            public GUIContent entryNameLabel;
            public GUIContent warningMessage;
            public float warningMessageHeight;

            public (TCollection collection, SharedTableData.SharedTableEntry entry) ? deferredSetReference;

            GUIContent m_FieldLabel;
            SharedTableData.SharedTableEntry m_SelectedEntry;
            TCollection m_SelectedTableCollection;
            int m_SelectedTableIdx = -1;

            public bool NeedsInitializing { get; protected set; } = true;

            public int SelectedTableIndex
            {
                get
                {
                    if (m_SelectedTableIdx < 0)
                    {
                        if (SelectedTableCollection == null)
                        {
                            m_SelectedTableIdx = 0;
                        }
                        else
                        {
                            for (int i = 1; i < ProjectTableLabels.Length; ++i)
                            {
                                if (SelectedTableCollection.TableCollectionName == ProjectTableLabels[i].text)
                                {
                                    m_SelectedTableIdx = i;
                                    break;
                                }
                            }
                        }
                    }
                    return m_SelectedTableIdx;
                }
                set
                {
                    m_SelectedTableIdx = value;
                    SelectedTableCollection = s_TableChoices[m_SelectedTableIdx];
                    m_FieldLabel = null;
                }
            }

            public TCollection SelectedTableCollection
            {
                get
                {
                    if (m_SelectedTableCollection == null && tableReference.Reference.ReferenceType != TableReference.Type.Empty)
                    {
                        var tableCollections = GetProjectTableCollections();
                        warningMessage = null;
                        if (tableReference.Reference.ReferenceType == TableReference.Type.Name)
                        {
                            m_SelectedTableCollection = tableCollections.FirstOrDefault(t => t.TableCollectionName == tableReference.Reference);
                            if (m_SelectedTableCollection == null)
                                warningMessage = new GUIContent($"Could not find a Table Collection with the name: {tableReference.Reference.TableCollectionName}");
                        }
                        else
                        {
                            m_SelectedTableCollection = tableCollections.FirstOrDefault(t => t.SharedData.TableCollectionNameGuid == tableReference.Reference);
                            if (m_SelectedTableCollection == null)
                                warningMessage = new GUIContent($"Could not find a Table Collection with the Guid: {tableReference.Reference.TableCollectionNameGuid}");
                        }
                    }
                    return m_SelectedTableCollection;
                }
                set
                {
                    m_SelectedTableCollection = value;
                    m_SelectedTableIdx = -1;
                    SelectedTableEntry = null;
                    warningMessage = null;
                    tableReference?.SetReference(value);
                }
            }

            public virtual SharedTableData.SharedTableEntry SelectedTableEntry
            {
                get
                {
                    if (m_SelectedEntry == null && SelectedTableCollection != null && tableEntryReference.Reference.ReferenceType != TableEntryReference.Type.Empty)
                    {
                        warningMessage = null;
                        if (tableEntryReference.Reference.ReferenceType == TableEntryReference.Type.Name)
                        {
                            m_SelectedEntry = m_SelectedTableCollection.SharedData.GetEntry(tableEntryReference.Reference.Key);
                            if (m_SelectedEntry == null)
                            {
                                warningMessage = new GUIContent($"The Table Collection `{SelectedTableCollection.TableCollectionName}` is Missing an entry with the Key: {tableEntryReference.Reference.Key}.");
                            }
                        }
                        else
                        {
                            m_SelectedEntry = m_SelectedTableCollection.SharedData.GetEntry(tableEntryReference.Reference.KeyId);
                            if (m_SelectedEntry == null)
                            {
                                warningMessage = new GUIContent($"The Table Collection `{SelectedTableCollection.TableCollectionName}` is Missing an entry with the Key Id: {tableEntryReference.Reference.KeyId}.");
                            }
                        }
                    }
                    return m_SelectedEntry;
                }
                set
                {
                    m_FieldLabel = null;
                    m_SelectedEntry = value;
                    tableEntryReference?.SetReference(value);
                }
            }

            public GUIContent FieldLabel
            {
                get
                {
                    if (m_FieldLabel == null)
                    {
                        if (tableReference.HasMultipleDifferentValues || tableEntryReference.HasMultipleDifferentValues)
                        {
                            m_FieldLabel = Styles.mixedValueContent;
                            return m_FieldLabel;
                        }

                        var icon = EditorGUIUtility.ObjectContent(null, assetType);
                        if (SelectedTableCollection != null && SelectedTableEntry != null)
                        {
                            var key = SelectedTableEntry.Key;
                            var eol = key.IndexOf('\n');
                            if (eol > 0)
                            {
                                // We don't want a multiline label as it overflows in the UI.
                                key = key.Substring(0, eol);
                            }
                            m_FieldLabel = new GUIContent($"{SelectedTableCollection.TableCollectionName}/{key}", icon.image);
                        }
                        else
                        {
                            m_FieldLabel = new GUIContent($"None ({assetType.Name})", icon.image);
                        }
                    }
                    return m_FieldLabel;
                }
            }

            public override void Reset()
            {
                serializedObject = null;
                tableReference = null;
                tableEntryReference = null;
                fallbackState = null;

                // Clear cached values
                m_FieldLabel = null;
                m_SelectedEntry = null;
                m_SelectedTableCollection = null;
                m_SelectedTableIdx = -1;

                NeedsInitializing = true;
            }

            public virtual void Init(SerializedProperty property)
            {
                rootProperty = property;
                serializedObject = property.serializedObject;
                tableReference = new SerializedTableReference(property.FindPropertyRelative("m_TableReference"));
                tableEntryReference = new SerializedTableEntryReference(property.FindPropertyRelative("m_TableEntryReference"));
                fallbackState = property.FindPropertyRelative("m_FallbackState");
                waitForCompletion = property.FindPropertyRelative("m_WaitForCompletion");
                NeedsInitializing = false;
            }
        }

        static GUIContent[] s_TableChoicesLabels;
        static TCollection[] s_TableChoices;

        const float k_OpenTableEditorButtonWidth = 30;

        static GUIContent[] ProjectTableLabels
        {
            get
            {
                if (s_TableChoicesLabels == null)
                {
                    var assetTables = GetProjectTableCollections();

                    s_TableChoices = new TCollection[assetTables.Count + 1];
                    assetTables.CopyTo(s_TableChoices, 1);

                    s_TableChoicesLabels = new GUIContent[assetTables.Count + 1];
                    s_TableChoicesLabels[0] = Styles.noTableSelected;
                    for (int i = 0; i < assetTables.Count; ++i)
                    {
                        s_TableChoicesLabels[i + 1] = new GUIContent(assetTables[i].TableCollectionName);
                    }
                }
                return s_TableChoicesLabels;
            }
        }

        public LocalizedReferencePropertyDrawer()
        {
            LocalizationEditorSettings.EditorEvents.CollectionAdded += EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved += EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.TableEntryRemoved += EditorEvents_CollectionEntryRemoved;
            LocalizationEditorSettings.EditorEvents.CollectionModified += EditorEvents_CollectionModifiedWithSender;
            LocalizationEditorSettings.EditorEvents.LocaleAdded += EditorEvents_LocaleAddedOrRemoved;
            LocalizationEditorSettings.EditorEvents.LocaleRemoved += EditorEvents_LocaleAddedOrRemoved;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        ~LocalizedReferencePropertyDrawer()
        {
            Dispose();
        }

        public void Dispose()
        {
            LocalizationEditorSettings.EditorEvents.CollectionAdded -= EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved -= EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.TableEntryRemoved -= EditorEvents_CollectionEntryRemoved;
            LocalizationEditorSettings.EditorEvents.CollectionModified -= EditorEvents_CollectionModifiedWithSender;
            LocalizationEditorSettings.EditorEvents.LocaleAdded -= EditorEvents_LocaleAddedOrRemoved;
            LocalizationEditorSettings.EditorEvents.LocaleRemoved -= EditorEvents_LocaleAddedOrRemoved;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        protected virtual void UndoRedoPerformed()
        {
            foreach (var prop in PropertyData.Values)
            {
                prop.Reset();
            }
        }

        void EditorEvents_CollectionEntryRemoved(LocalizationTableCollection collection, SharedTableData.SharedTableEntry entry)
        {
            s_TableChoicesLabels = null;
            s_TableChoices = null;
            ClearPropertyDataCache();
        }

        void EditorEvents_CollectionModifiedWithSender(object sender, LocalizationTableCollection collection)
        {
            if (sender != this)
                EditorEvents_CollectionModified(collection);
        }

        void EditorEvents_CollectionModified(LocalizationTableCollection obj)
        {
            s_TableChoicesLabels = null;
            s_TableChoices = null;
            ClearPropertyDataCache();
        }

        void EditorEvents_LocaleAddedOrRemoved(Locale obj)
        {
            s_TableChoicesLabels = null;
            s_TableChoices = null;
            ClearPropertyDataCache();
        }

        public override Data GetDataForProperty(SerializedProperty property)
        {
            var data = base.GetDataForProperty(property);
            if (data.NeedsInitializing)
                data.Init(property);
            return data;
        }

        public override Data CreatePropertyData(SerializedProperty property) => new Data { entryNameLabel = Styles.entryName };

        public override void OnGUI(Data data, Rect position, SerializedProperty property, GUIContent label)
        {
            var rowPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            var foldoutRect = new Rect(rowPosition.x, rowPosition.y, PrefixLabelWidth, EditorGUIUtility.singleLineHeight);
            var dropDownPosition = new Rect(foldoutRect.xMax, rowPosition.y, rowPosition.width - PrefixLabelWidth, EditorGUIUtility.singleLineHeight);
            rowPosition.MoveToNextLine();

            EditorGUI.BeginProperty(foldoutRect, label, property);

            if (data.deferredSetReference.HasValue)
            {
                data.SelectedTableCollection = data.deferredSetReference.Value.collection;
                data.SelectedTableEntry = data.deferredSetReference.Value.entry;
                data.deferredSetReference = null;
                GUI.changed = true;
            }

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            if (EditorGUI.DropdownButton(dropDownPosition, data.FieldLabel, FocusType.Passive))
            {
                ShowPicker(data, dropDownPosition);
            }
            EditorGUI.EndProperty();

            // Missing table collection warning
            if (data.warningMessage != null)
            {
                rowPosition.height = data.warningMessageHeight;
                EditorGUI.HelpBox(rowPosition, data.warningMessage.text, MessageType.Warning);
                rowPosition.MoveToNextLine();
                rowPosition.height = EditorGUIUtility.singleLineHeight;
            }

            DrawTableDetails(position, rowPosition, data, property);
        }

        protected virtual void ShowPicker(Data data, Rect dropDownPosition)
        {
            var treeSelection = new TableEntryTreeView(data.assetType, (collection, entry) =>
            {
                data.deferredSetReference = (collection as TCollection, entry);
            });

            PopupWindow.Show(dropDownPosition, new TreeViewPopupWindow(treeSelection) { Width = dropDownPosition.width });
        }

        void DrawTableDetails(Rect position, Rect rowPosition, Data data, SerializedProperty property)
        {
            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;

            // Table selection
            var tableSelectionPos = data.SelectedTableIndex != 0 ? new Rect(rowPosition.x, rowPosition.y, rowPosition.width - k_OpenTableEditorButtonWidth, rowPosition.height) : rowPosition;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(tableSelectionPos, Styles.selectedTable, data.tableReference.TableNameProperty);
            var selectedTableIndex = EditorGUI.Popup(tableSelectionPos, Styles.selectedTable, data.SelectedTableIndex, ProjectTableLabels);
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                data.SelectedTableIndex = selectedTableIndex;
            }

            if (data.SelectedTableIndex != 0)
            {
                var openTableEditorPos = new Rect(tableSelectionPos.xMax, tableSelectionPos.y, k_OpenTableEditorButtonWidth, tableSelectionPos.height);
                if (GUI.Button(openTableEditorPos, EditorIcons.TableWindow))
                {
                    LocalizationTablesWindow.ShowWindow(data.tableReference.Reference, data.tableEntryReference.Reference);
                }
            }

            rowPosition.y += rowPosition.height;
            var buttonPos = new Rect(rowPosition.x + PrefixLabelWidth, rowPosition.y, rowPosition.width - PrefixLabelWidth, rowPosition.height);
            if (selectedTableIndex == 0)
            {
                if (GUI.Button(buttonPos, Styles.addTableCollection, EditorStyles.miniButton))
                {
                    TableCreatorWindow.ShowWindow();
                }
            }
            else
            {
                if (GUI.Button(buttonPos, Styles.addTableEntry, EditorStyles.miniButton))
                {
                    // Attempt to create a new key with a set default name
                    var keys = data.SelectedTableCollection.SharedData;

                    Undo.RecordObject(keys, "Add entry.");
                    var entry = keys.AddKey();
                    EditorUtility.SetDirty(keys);

                    data.SelectedTableEntry = entry;
                    LocalizationEditorSettings.EditorEvents.RaiseTableEntryAdded(data.SelectedTableCollection, entry);
                }
            }

            rowPosition.y += rowPosition.height;
            buttonPos.y = rowPosition.y;
            if (GUI.Button(buttonPos, Styles.addLocale))
            {
                LocaleGeneratorWindow.ShowWindow();
            }
            rowPosition.MoveToNextLine();

            EditorGUI.PropertyField(rowPosition, data.fallbackState, Styles.useFallback);
            rowPosition.MoveToNextLine();
            EditorGUI.PropertyField(rowPosition, data.waitForCompletion, Styles.waitForCompletion);
            rowPosition.MoveToNextLine();

            if (data.SelectedTableEntry != null)
                DrawTableEntryDetails(ref rowPosition, data, position);
            EditorGUI.indentLevel--;
        }

        protected virtual void DrawTableEntryDetails(ref Rect rowPosition, Data data, Rect position)
        {
            // Entry name
            EditorGUI.BeginChangeCheck();
            var textAreaPosition = EditorGUI.PrefixLabel(rowPosition, data.entryNameLabel);
            rowPosition.height = textAreaPosition.height = EditorStyles.textArea.CalcHeight(new GUIContent(data.SelectedTableEntry?.Key), textAreaPosition.width);

            // Fix indent issue with TextArea
            EditorGUI.indentLevel--;
            var newName = EditorGUI.TextArea(textAreaPosition, data.SelectedTableEntry?.Key);
            EditorGUI.indentLevel++;

            rowPosition.MoveToNextLine();
            if (EditorGUI.EndChangeCheck() && data.SelectedTableCollection != null)
            {
                // Prevent renaming to a name that is already taken.
                var sharedData = data.SelectedTableCollection.SharedData;
                var entry = sharedData.GetEntry(newName);

                if (string.IsNullOrEmpty(newName))
                {
                    data.entryNameLabel = new GUIContent(Styles.entryName.text, EditorIcons.WarningIcon, "Names can not be empty.");
                }
                else if (entry == null || data.SelectedTableEntry == entry)
                {
                    Undo.RecordObject(sharedData, "Rename key entry");
                    sharedData.RenameKey(data.SelectedTableEntry.Key, newName);
                    EditorUtility.SetDirty(sharedData);
                    data.entryNameLabel = Styles.entryName;

                    LocalizationEditorSettings.EditorEvents.RaiseTableEntryModified(entry);
                }
                else
                {
                    data.entryNameLabel = new GUIContent(Styles.entryName.text, EditorIcons.WarningIcon, $"Can not rename key to '{newName}', the name is already in use by Key Id {entry.Id}.");
                }
            }
        }

        public override float GetPropertyHeight(Data data, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout field height

            if (data.warningMessage != null)
            {
                data.warningMessageHeight = EditorStyles.helpBox.CalcHeight(data.warningMessage, EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth);
                height += data.warningMessageHeight;
            }

            if (property.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight; // Selected table
                height += EditorGUIUtility.singleLineHeight; // create table/add entry button
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Add locale button
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2; // Fallback & Wait For Completion

                if (data.SelectedTableEntry != null)
                {
                    // Entry name
                    var labelwidth = EditorGUI.PrefixLabel(new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight), data.entryNameLabel).width;
                    height += EditorStyles.textArea.CalcHeight(new GUIContent(data.SelectedTableEntry?.Key), labelwidth) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            return height;
        }
    }
}
