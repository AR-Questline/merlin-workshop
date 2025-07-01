using System;
using System.Collections.Generic;
using CsvHelper;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.CSV.Columns
{
    /// <summary>
    /// Represents the localized value and <see cref="Comment"/> metadata from a <see cref="StringTable"/>.
    /// </summary>
    [Serializable]
    public class LocaleColumns : CsvColumns, ISerializationCallbackReceiver
    {
        [SerializeField] LocaleIdentifier m_LocaleIdentifier;

        [Tooltip("The name of the value Column that should be used as the header when exporting and mapped against when importing.")]
        [SerializeField] string m_FieldName;

        [Tooltip("The name of the Comment Column that should be used as the header when exporting and mapped against when importing.")]
        [SerializeField] string m_CommentFieldName;

        [Tooltip("Should the Comment metadata be included as a Column?")]
        [SerializeField] bool m_IncludeComments = false;

        int m_CollectionTableIndex, m_FieldIndex, m_CommentFieldIndex;
        StringTable m_ImportTable;

        /// <summary>
        /// Used to extract the <see cref="StringTable"/> from the <see cref="StringTableCollection"/> when importing and exporting.
        /// </summary>
        public LocaleIdentifier LocaleIdentifier
        {
            get => m_LocaleIdentifier;
            set
            {
                m_LocaleIdentifier = value;
                SetDefaultFieldNames();
            }
        }

        /// <summary>
        /// The value field name when exporting and importing.
        /// When importing this name will be checked against the headers to determine if the field is present in the CSV data.
        /// </summary>
        public string FieldName
        {
            get => m_FieldName;
            set => m_FieldName = value;
        }

        /// <summary>
        /// The comments field name when exporting and importing.
        /// When importing this name will be checked against the headers to determine if the field is present in the CSV data.
        /// </summary>
        public string CommentFieldName
        {
            get => m_CommentFieldName;
            set => m_CommentFieldName = value;
        }

        /// <summary>
        /// Should the <see cref="Comment"/> metadata be exported as a separate column?
        /// </summary>
        public bool IncludeComments
        {
            get => m_IncludeComments;
            set => m_IncludeComments = value;
        }

        /// <inheritdoc/>
        public override void WriteBegin(StringTableCollection collection, CsvWriter csvWriter)
        {
            var tables = collection.StringTables;
            m_CollectionTableIndex = -1;
            for (int i = 0; i < tables.Count; ++i)
            {
                if (tables[i].LocaleIdentifier == LocaleIdentifier)
                {
                    m_CollectionTableIndex = i;
                }
            }

            if (m_CollectionTableIndex != -1)
            {
                csvWriter.WriteField(FieldName);

                if (IncludeComments)
                    csvWriter.WriteField(CommentFieldName);
            }
        }

        /// <inheritdoc/>
        public override void ReadBegin(StringTableCollection collection, CsvReader csvReader)
        {
            m_ImportTable = collection.GetTable(LocaleIdentifier) as StringTable;
            if (m_ImportTable != null)
            {
                m_FieldIndex = csvReader.GetFieldIndex(FieldName, isTryGet: true);
                m_CommentFieldIndex = csvReader.GetFieldIndex(CommentFieldName, isTryGet: true);
            }
            else
            {
                m_FieldIndex = -1;
                m_CommentFieldIndex = -1;
            }
        }

        /// <inheritdoc/>
        public override void ReadEnd(StringTableCollection collection)
        {
            m_ImportTable = null;
        }

        /// <inheritdoc/>
        public override void ReadRow(SharedTableData.SharedTableEntry keyEntry, CsvReader reader)
        {
            if (m_ImportTable == null)
                return;

            StringTableEntry entry = m_ImportTable.GetEntry(keyEntry.Id);
            if (m_FieldIndex != -1)
            {
                var text = reader.GetField(m_FieldIndex);
                if (string.IsNullOrEmpty(text))
                {
                    // Dont create a new entry if the value is empty
                    if (entry != null)
                        entry.Data.Localized = string.Empty;
                }
                else
                {
                    if (entry == null)
                        entry = m_ImportTable.AddEntry(keyEntry.Id, text);
                    else
                        entry.Data.Localized = text;
                }
            }
            else
            {
                entry = m_ImportTable.GetEntry(keyEntry.Id) ?? m_ImportTable.AddEntry(keyEntry.Id, string.Empty);
            }

            if (m_CommentFieldIndex != -1)
            {
                var commentText = reader.GetField(m_CommentFieldIndex);
                if (string.IsNullOrEmpty(commentText))
                {
                    // Remove any comment that exists
                    var comment = entry?.GetMetadata<Comment>();
                    if (comment != null)
                        entry.RemoveMetadata(comment);
                }
                else
                {
                    if (entry == null)
                        entry = m_ImportTable.AddEntry(keyEntry.Id, string.Empty);

                    var comment = entry.GetMetadata<Comment>();
                    if (comment == null)
                    {
                        comment = new Comment();
                        entry.AddMetadata(comment);
                    }

                    comment.CommentText = commentText;
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteRow(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, CsvWriter writer)
        {
            if (m_CollectionTableIndex == -1)
                return;

            if (tableEntries[m_CollectionTableIndex] != null)
            {
                var entry = tableEntries[m_CollectionTableIndex];
                writer.WriteField(entry.LocalizedValue, true);

                if (IncludeComments)
                {
                    var comment = entry.GetMetadata<Comment>();
                    writer.WriteField(comment?.CommentText, true);
                }
            }
            else
            {
                writer.WriteField(string.Empty);
                if (IncludeComments)
                    writer.WriteField(string.Empty);
            }
        }

        /// <summary>
        /// Sets the field names to their default values.
        /// The default values are <see cref="LocaleIdentifier.ToString"/> for <see cref="FieldName"/>
        /// and <see cref="FieldName"/> + " Comments" for <see cref="CommentFieldName"/>.
        /// </summary>
        public void SetDefaultFieldNames()
        {
            if (!string.IsNullOrEmpty(LocaleIdentifier.Code))
            {
                if (string.IsNullOrEmpty(FieldName))
                {
                    FieldName = LocaleIdentifier.ToString();
                }

                if (string.IsNullOrEmpty(CommentFieldName))
                {
                    CommentFieldName = FieldName + " Comments";
                }
            }
            else
            {
                FieldName = string.Empty;
                CommentFieldName = string.Empty;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            SetDefaultFieldNames();
        }
    }
}
