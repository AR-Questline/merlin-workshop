using System;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    abstract class LocalizedTablePropertyDrawer<TCollection> : PropertyDrawerExtended<LocalizedTablePropertyDrawer<TCollection>.LocalizedTablePropertyDrawerPropertyData>, IDisposable
        where TCollection : LocalizationTableCollection
    {
        public class LocalizedTablePropertyDrawerPropertyData : PropertyDrawerExtendedData
        {
            GUIContent m_FieldLabel;
            TCollection m_SelectedTableCollection;

            public SerializedObject serializedObject;
            public SerializedTableReference tableReference;
            public Type assetType;
            public GUIContent warningMessage;
            public float warningMessageHeight;

            public bool collectionSet = false;
            public TCollection deferredCollection;

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
                    m_FieldLabel = null;
                    warningMessage = null;
                    tableReference.SetReference(value);
                }
            }

            public GUIContent FieldLabel
            {
                get
                {
                    if (m_FieldLabel == null)
                    {
                        if (SelectedTableCollection != null)
                        {
                            var icon = EditorGUIUtility.ObjectContent(m_SelectedTableCollection, assetType);
                            m_FieldLabel = new GUIContent(SelectedTableCollection.TableCollectionName, icon.image);
                        }
                        else
                        {
                            m_FieldLabel = new GUIContent($"None ({ObjectNames.NicifyVariableName(assetType.Name)})");
                        }
                    }
                    return m_FieldLabel;
                }
            }
        }

        protected static Func<ReadOnlyCollection<TCollection>> GetProjectTableCollections { get; set; }

        public LocalizedTablePropertyDrawer()
        {
            LocalizationEditorSettings.EditorEvents.CollectionAdded += EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved += EditorEvents_CollectionModified;
            Undo.undoRedoPerformed += ClearPropertyDataCache;
        }

        ~LocalizedTablePropertyDrawer()
        {
            Dispose();
        }

        public void Dispose()
        {
            LocalizationEditorSettings.EditorEvents.CollectionAdded -= EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved -= EditorEvents_CollectionModified;
            Undo.undoRedoPerformed -= ClearPropertyDataCache;
        }

        void EditorEvents_CollectionModified(LocalizationTableCollection obj) => ClearPropertyDataCache();

        public override LocalizedTablePropertyDrawerPropertyData CreatePropertyData(SerializedProperty property)
        {
            return new LocalizedTablePropertyDrawerPropertyData
            {
                serializedObject = property.serializedObject,
                tableReference = new SerializedTableReference(property.FindPropertyRelative("m_TableReference")),
                assetType = typeof(TCollection) == typeof(StringTableCollection) ? typeof(StringTable) : typeof(AssetTable)
            };
        }

        public override void OnGUI(LocalizedTablePropertyDrawerPropertyData data, Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            var dropDownPosition = EditorGUI.PrefixLabel(position, label);

            // We defer setting so we can set it during the IMGUI call. This way ApplyModifiedProperties will detect the change.
            if (data.collectionSet)
            {
                data.SelectedTableCollection = data.deferredCollection;
                data.deferredCollection = null;
                data.collectionSet = false;
                GUI.changed = true;
            }

            if (EditorGUI.DropdownButton(dropDownPosition, data.FieldLabel, FocusType.Passive))
            {
                var treeSelection = new TableTreeView(typeof(TCollection) == typeof(StringTableCollection) ? typeof(StringTable) : typeof(AssetTable), collection =>
                {
                    data.deferredCollection = collection as TCollection;
                    data.collectionSet = true;
                });

                PopupWindow.Show(dropDownPosition, new TreeViewPopupWindow(treeSelection) { Width = dropDownPosition.width });
            }

            // Missing table collection warning
            if (data.warningMessage != null)
            {
                position.MoveToNextLine();
                position.height = data.warningMessageHeight;
                EditorGUI.HelpBox(position, data.warningMessage.text, MessageType.Warning);
            }
        }

        public override float GetPropertyHeight(LocalizedTablePropertyDrawerPropertyData data, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (data.warningMessage != null)
            {
                data.warningMessageHeight = EditorStyles.helpBox.CalcHeight(data.warningMessage, EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth);
                height += data.warningMessageHeight;
            }

            return height;
        }
    }
}
