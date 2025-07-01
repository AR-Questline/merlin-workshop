using System;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// Provides a column that can syncronize the <see cref="Comment"/> metadata from the <see cref="SharedTableData.SharedTableEntry"/>.
    /// </summary>
    [Serializable]
    public class KeyCommentColumn : KeyMetadataColumn<Comment>
    {
        /// <inheritdoc/>
        public override PushFields PushFields => PushFields.Value;

        /// <summary>
        /// Default column name "Key Comments".
        /// </summary>
        public const string ColumnHeader = "Key Comments";

        /// <inheritdoc/>
        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = ColumnHeader;
            headerNote = null;
        }

        /// <inheritdoc/>
        public override void PushMetadata(Comment metadata, out string value, out string note)
        {
            value = metadata.CommentText;
            note = null;
        }

        /// <inheritdoc/>
        public override void PullMetadata(SharedTableData.SharedTableEntry keyEntry, Comment metadata, string cellValue, string cellNote)
        {
            if (string.IsNullOrEmpty(cellValue))
            {
                if (metadata != null)
                    keyEntry.Metadata.RemoveMetadata(metadata);
            }
            else
            {
                if (metadata != null)
                    metadata.CommentText = cellValue;
                else
                    keyEntry.Metadata.AddMetadata(new Comment { CommentText = cellValue });
            }
        }
    }
}
