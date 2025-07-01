using System;
using UnityEngine;

namespace UnityEditor.Localization.Plugins.Google
{
    /// <summary>
    /// Contains settings for any newly created Google Sheet.
    /// </summary>
    [Serializable]
    public class NewSheetProperties
    {
        [SerializeField]
        Color m_HeaderForegroundColor = new Color(0.8941177f, 0.8941177f, 0.8941177f);

        [SerializeField]
        Color m_HeaderBackgroundColor = new Color(0.2196079f, 0.2196079f, 0.2196079f);

        [SerializeField]
        Color m_DuplicateKeyColor = new Color(0.8745098f, 0.2240707f, 0.1921569f);

        [SerializeField]
        bool m_HighlightDuplicateKeys = true;

        [SerializeField]
        bool m_FreezeTitleRowAndKeyColumn = true;

        /// <summary>
        /// The color to use for the header row foreground.
        /// </summary>
        public Color HeaderForegroundColor { get => m_HeaderForegroundColor; set => m_HeaderForegroundColor = value; }

        /// <summary>
        /// The color to use for the header row background.
        /// </summary>
        public Color HeaderBackgroundColor { get => m_HeaderBackgroundColor; set => m_HeaderBackgroundColor = value; }

        /// <summary>
        /// The color to use when highlighting duplicate keys. See also <seealso cref="HighlightDuplicateKeys"/>
        /// </summary>
        public Color DuplicateKeyColor { get => m_DuplicateKeyColor; set => m_DuplicateKeyColor = value; }

        /// <summary>
        /// Should duplicate keys be highlighted with <see cref="DuplicateKeyColor"/> in the sheet?
        /// </summary>
        public bool HighlightDuplicateKeys { get => m_HighlightDuplicateKeys; set => m_HighlightDuplicateKeys = value; }

        /// <summary>
        /// Freeze the top row and first column which are typically the column title row and key column.
        /// </summary>
        public bool FreezeTitleRowAndKeyColumn { get => m_FreezeTitleRowAndKeyColumn; set => m_FreezeTitleRowAndKeyColumn = value; }
    }
}
