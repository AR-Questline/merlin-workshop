using System;
using UnityEngine.Localization.Metadata;

namespace UnityEngine.Localization.Tables
{
    [Serializable]
    class TableEntryData
    {
        [SerializeField]
        long m_Id;

        [SerializeField]
        string m_Localized;

        [SerializeField]
        MetadataCollection m_Metadata = new MetadataCollection();

        /// <summary>
        /// The Key for this table entry. Must be unique for each table.
        /// </summary>
        public long Id
        {
            get => m_Id;
            set => m_Id = value;
        }

        /// <summary>
        /// This can be either a translated string or for assets the guid.
        /// </summary>
        public string Localized
        {
            get => m_Localized;
            set => m_Localized = value;
        }

        /// <summary>
        /// The metadata for this entry.
        /// </summary>
        public MetadataCollection Metadata
        {
            get => m_Metadata;
            set => m_Metadata = value;
        }

        public TableEntryData() {}

        /// <summary>
        /// TODO: DOC
        /// </summary>
        /// <param name="id"></param>
        public TableEntryData(long id) => Id = id;

        /// <summary>
        /// TODO: DOC
        /// </summary>
        /// <param name="id"></param>
        /// <param name="localized"></param>
        public TableEntryData(long id, string localized) : this(id)
        {
            Localized = localized;
        }
    }
}
