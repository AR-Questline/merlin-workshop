using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// Represents a Google Sheet column that is synchronized to the <see cref="Comment"/> metadata for a chosen <see cref="Locale"/>.
    /// </summary>
    [Serializable]
    public class LocaleCommentColumn : LocaleMetadataColumn<Comment>
    {
        /// <inheritdoc/>
        public override PushFields PushFields => PushFields.Value;

        /// <inheritdoc/>
        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = $"{LocaleIdentifier.ToString()} Comments";
            headerNote = null;
        }

        /// <inheritdoc/>
        public override void PushMetadata(Comment metadata, out string value, out string note)
        {
            value = metadata.CommentText;
            note = null;
        }

        /// <inheritdoc/>
        public override void PullMetadata(StringTableEntry entry, Comment metadata, string cellValue, string cellNote)
        {
            if (string.IsNullOrEmpty(cellValue))
            {
                if (metadata != null)
                    entry.RemoveMetadata(metadata);

                // If the entry is empty then just remove the whole thing
                if (string.IsNullOrEmpty(entry.Value) && entry.MetadataEntries.Count == 0)
                    entry.RemoveFromTable();
            }
            else
            {
                if (metadata != null)
                    metadata.CommentText = cellValue;
                else
                    entry.AddMetadata(new Comment { CommentText = cellValue });
            }
        }
    }
}
