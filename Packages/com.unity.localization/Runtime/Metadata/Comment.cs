using System;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Used to add metadata comments.
    /// Comments can be any type of information but are particularly useful for providing context to translators.
    /// </summary>
    [Metadata]
    [Serializable]
    public class Comment : IMetadata
    {
        [SerializeField]
        [TextArea(1, int.MaxValue)]
        string m_CommentText = "Comment Text";

        /// <summary>
        /// The comment text.
        /// </summary>
        public string CommentText
        {
            get => m_CommentText;
            set => m_CommentText = value;
        }

        public override string ToString() => CommentText;
    }
}
