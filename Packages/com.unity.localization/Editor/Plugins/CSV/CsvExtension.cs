using System;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEngine;

namespace UnityEditor.Localization.Plugins.CSV
{
    /// <summary>
    /// Provides an editor interface and configuration to a Comma Separated Values (CSV) file.
    /// </summary>
    [Serializable]
    [StringTableCollectionExtension]
    public class CsvExtension : CollectionExtension
    {
        [SerializeReference]
        List<CsvColumns> m_ColumnsList;

        [SerializeField]
        string m_ConnectedFile;

        /// <summary>
        /// Configuation for which mapping CSV columns to their respective localization data.
        /// </summary>
        public List<CsvColumns> Columns => m_ColumnsList;

        /// <summary>
        /// Path to the CSV file that will be used by default.
        /// </summary>
        public string File
        {
            get => m_ConnectedFile;
            set => m_ConnectedFile = value;
        }

        /// <summary>
        /// Initializes the CSV extension with <see cref="ColumnMapping.CreateDefaultMapping(bool)"/>.
        /// </summary>
        public override void Initialize()
        {
            m_ColumnsList = ColumnMapping.CreateDefaultMapping();
        }
    }
}
