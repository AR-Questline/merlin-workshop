using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Displays all the asset tables for the project collated by type.
    /// </summary>
    #if UNITY_2023_3_OR_NEWER
    [UxmlElement]
    #endif
    partial class ProjectTablesPopup : PopupField<LocalizationTableCollection>, IDisposable
    {
        internal class NoTables : LocalizationTableCollection
        {
            protected internal override Type TableType => null;
            protected internal override Type RequiredExtensionAttribute => null;

            protected internal override string DefaultGroupName => "No Tables";

            public override string ToString() => k_NoTablesMessage;

            public override void RemoveEntry(TableEntryReference entryReference)
            {}
        }

        const string k_EditorPrefValueKey = "Localization-SelectedAssetTable";
        const string k_NoTablesMessage = "No Tables Found. Please Create One";

        #if !UNITY_2023_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<ProjectTablesPopup> {}
        #endif

        static readonly NoTables k_NoTables = NoTables.CreateInstance<NoTables>();
        static readonly List<LocalizationTableCollection> s_Tables = new List<LocalizationTableCollection>();

        public ProjectTablesPopup()
            : base(GetChoices(), GetDefaultIndex(), FormatSelectedLabel, FormatListLabel)
        {
            label = "Selected collection";

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                LocalizationEditorSettings.EditorEvents.CollectionAdded += OnCollectionAdded;
                LocalizationEditorSettings.EditorEvents.CollectionRemoved += OnCollectionRemoved;
                LocalizationEditorSettings.EditorEvents.CollectionModified += OnCollectionModified;
            });

            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                ((IDisposable)this).Dispose();
            });
        }

        void IDisposable.Dispose()
        {
            LocalizationEditorSettings.EditorEvents.CollectionAdded -= OnCollectionAdded;
            LocalizationEditorSettings.EditorEvents.CollectionRemoved -= OnCollectionRemoved;
            LocalizationEditorSettings.EditorEvents.CollectionModified -= OnCollectionModified;
        }

        void OnCollectionModified(object called, LocalizationTableCollection col)
        {
            OnCollectionAdded(col);
        }

        void OnCollectionAdded(LocalizationTableCollection col)
        {
            bool isEmpty = value is NoTables;
            GetChoices();

            // If we currently have no tables then select the new collection.
            if (isEmpty)
                value = col;
        }

        void OnCollectionRemoved(LocalizationTableCollection col)
        {
            var choices = GetChoices();

            if (value == col)
                value = choices[0];
        }

        static int GetDefaultIndex()
        {
            var selection = EditorPrefs.GetString(k_EditorPrefValueKey, null);
             if (!string.IsNullOrEmpty(selection))
            {
                for (int i = 0; i < s_Tables.Count; ++i)
                {
                    if (s_Tables[i]?.ToString() == selection)
                        return i;
                }
            }

            return 0;
        }

        public override LocalizationTableCollection value
        {
            get => base.value;
            set
            {
                if (value == null)
                    EditorPrefs.DeleteKey(k_EditorPrefValueKey);
                else
                    EditorPrefs.SetString(k_EditorPrefValueKey, value.ToString());
                base.value = value;
            }
        }

        public void RefreshLabels()
        {
            GetChoices();
            var newValue = Mathf.Clamp(s_Tables.FindIndex(o => value.Equals(o)), 0, s_Tables.Count);
            SetValueWithoutNotify(s_Tables[newValue]);
        }

        static string FormatListLabel(LocalizationTableCollection atc)
        {
            return atc is NoTables ? atc.ToString() : $"{atc.Group}/{atc.TableCollectionName}";
        }

        static string FormatSelectedLabel(LocalizationTableCollection atc)
        {
            if (atc != null)
                return atc.ToString();
            return k_NoTablesMessage;
        }

        static List<LocalizationTableCollection> GetChoices()
        {
            s_Tables.Clear();
            s_Tables.AddRange(LocalizationEditorSettings.Instance.TableCollectionCache.StringTableCollections);
            s_Tables.AddRange(LocalizationEditorSettings.Instance.TableCollectionCache.AssetTableCollections);

            if (s_Tables.Count == 0)
                s_Tables.Add(k_NoTables);
            return s_Tables;
        }
    }
}
