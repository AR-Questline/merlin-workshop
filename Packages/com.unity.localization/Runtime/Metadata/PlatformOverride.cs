using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Controls how the entry should be overridden when using <see cref="IEntryOverride"/>.
    /// </summary>
    public enum EntryOverrideType
    {
        /// <summary>
        /// No override will be applied.
        /// </summary>
        None,

        /// <summary>
        /// The same entry will be used but from a different table.
        /// </summary>
        Table,

        /// <summary>
        /// The same table will be used but a different entry.
        /// </summary>
        Entry,

        /// <summary>
        /// An entry from a different table will be used.
        /// </summary>
        TableAndEntry
    }

    /// <summary>
    /// Metadata that can be applied to <see cref="SharedTableData.SharedTableEntry"/> or a table entry to override the entry when loading a localized value.
    /// </summary>
    /// <remarks>
    /// When fetching a localized value, an Entry Override can be used to redirect to a different table entry, such as when running on a certain platform or in a specific region.
    /// The Entry Override is evaluated during <see cref="LocalizedDatabase{TTable, TEntry}.GetTableEntryAsync(TableReference, TableEntryReference, Locale, FallbackBehavior)"/>.
    /// The table entry will first be checked for an override and then the <see cref="SharedTableData.SharedTableEntry"/>.
    /// ![](../manual/images/GetEntry.dot.svg)
    /// See also <seealso cref="PlatformOverride"/>
    /// </remarks>
    /// <example>
    /// This example shows how to create an override that will be applied on a chosen day of the week.
    /// <code source="../../DocCodeSamples.Tests/PlatformOverrideExamples.cs" region="custom-entry-override"/>
    /// </example>
    public interface IEntryOverride : IMetadata
    {
        /// <summary>
        /// Determines if the table, entry or both should be overridden.
        /// </summary>
        /// <param name="tableReference">The table to use or <see langword="default"/> if it is not overriden.</param>
        /// <param name="tableEntryReference">The entry to use or <see langword="default"/> if it is not overriden.</param>
        /// <returns>Returns the fields that should be overridden.</returns>
        EntryOverrideType GetOverride(out TableReference tableReference, out TableEntryReference tableEntryReference);
    }

    /// <summary>
    /// <see cref="Metadata"/> that can be used to redirect the localization system to a different table entry based on the current runtime platform.
    /// This <see cref="Metadata"/> can be applied to <see cref="SharedTableData.SharedTableEntry"/>, <see cref="StringTableEntry"/> or <see cref="AssetTableEntry"/>.
    /// </summary>
    [Metadata(AllowedTypes = MetadataType.AllSharedTableEntries | MetadataType.AllTableEntries, AllowMultiple = false)]
    [Serializable]
    public class PlatformOverride : IEntryOverride, ISerializationCallbackReceiver
    {
        [Serializable]
        class PlatformOverrideData
        {
            public RuntimePlatform platform;
            public EntryOverrideType entryOverrideType;
            public TableReference tableReference;
            public TableEntryReference tableEntryReference;

            public override string ToString()
            {
                switch (entryOverrideType)
                {
                    case EntryOverrideType.Table:         return $"{platform}: {tableReference}";
                    case EntryOverrideType.Entry:         return $"{platform}: {tableEntryReference}";
                    case EntryOverrideType.TableAndEntry: return $"{platform}: {tableReference}/{tableEntryReference}";
                }
                return $"{platform}: None";
            }
        }

        [SerializeField]
        List<PlatformOverrideData> m_PlatformOverrides = new List<PlatformOverrideData>();

        #if !UNITY_EDITOR
        PlatformOverrideData m_PlayerPlatformOverride;
        #endif

        /// <summary>
        /// Add a platform override for the current table collection.
        /// This will result in the table being switched but the same entry name being used.
        /// This is useful if you want to have specialist tables that will implement the same keys for certain entries.
        /// </summary>
        /// <example>
        /// This example shows how you could set up platform overrides using a table for each platform.
        /// <code source="../../DocCodeSamples.Tests/PlatformOverrideExamples.cs" region="table-override"/>
        /// </example>
        /// <param name="platform">The platform to override.</param>
        /// <param name="table">The table collection to use instead of the current one.</param>
        public void AddPlatformTableOverride(RuntimePlatform platform, TableReference table) => AddPlatformOverride(platform, table, default, EntryOverrideType.Table);

        /// <summary>
        /// Add a platform override for the current entry. This will use the same table collection but a different entry in the table than the one this Metadata is attached to.
        /// This is useful if you want to have overrides for entries that are stored in the same table.
        /// </summary>
        /// <example>
        /// This example shows how you could set up platform overrides using a single table.
        /// <code source="../../DocCodeSamples.Tests/PlatformOverrideExamples.cs" region="entry-override"/>
        /// </example>
        /// <param name="platform">The platform to override.</param>
        /// <param name="entry">The entry to use instead of the current one.</param>
        public void AddPlatformEntryOverride(RuntimePlatform platform, TableEntryReference entry) => AddPlatformOverride(platform, default, entry, EntryOverrideType.Entry);

        /// <summary>
        /// Add a platform override for the table, entry or both.
        /// </summary>
        /// <param name="platform">The platform to override.</param>
        /// <param name="table">The table collection to use instead of the current one.</param>
        /// <param name="entry">The entry to use instead of the current one.</param>
        /// <param name="entryOverrideType">Flags to insidcate the type of override to apply, table, entry or both.</param>
        public void AddPlatformOverride(RuntimePlatform platform, TableReference table, TableEntryReference entry, EntryOverrideType entryOverrideType = EntryOverrideType.TableAndEntry)
        {
            PlatformOverrideData platformOverrideData = null;
            for (int i = 0; i < m_PlatformOverrides.Count; ++i)
            {
                if (m_PlatformOverrides[i].platform == platform)
                {
                    platformOverrideData = m_PlatformOverrides[i];
                    break;
                }
            }

            if (platformOverrideData == null)
            {
                platformOverrideData = new PlatformOverrideData { platform = platform };
                m_PlatformOverrides.Add(platformOverrideData);
            }

            platformOverrideData.entryOverrideType = entryOverrideType;
            platformOverrideData.tableReference = table;
            platformOverrideData.tableEntryReference = entry;
        }

        /// <summary>
        /// Removes the platform override for the chosen platform.
        /// </summary>
        /// <param name="platform">The platform to remove.</param>
        /// <returns><see langword="true"/> if the platform was removed or <see langword="false"/> if no override was found.</returns>
        public bool RemovePlatformOverride(RuntimePlatform platform)
        {
            for (int i = 0; i < m_PlatformOverrides.Count; ++i)
            {
                if (m_PlatformOverrides[i].platform == platform)
                {
                    m_PlatformOverrides.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the <see cref="EntryOverrideType"/> for the platform the application is currently running on using [Application.platform](https://docs.unity3d.com/ScriptReference/Application-platform.html).
        /// </summary>
        /// <param name="tableReference">The table to use or <see langword="default"/> if it is not overriden.</param>
        /// <param name="tableEntryReference">The entry to use or <see langword="default"/> if it is not overriden.</param>
        /// <returns>Returns the fields that should be overridden.</returns>
        public EntryOverrideType GetOverride(out TableReference tableReference, out TableEntryReference tableEntryReference)
        {
            #if UNITY_EDITOR
            return GetOverride(out tableReference, out tableEntryReference, LocalizationSettings.Instance.Platform);
            #else
            if (m_PlayerPlatformOverride == null)
            {
                tableReference = default;
                tableEntryReference = default;
                return EntryOverrideType.None;
            }
            else
            {
                tableReference = m_PlayerPlatformOverride.tableReference;
                tableEntryReference = m_PlayerPlatformOverride.tableEntryReference;
                return m_PlayerPlatformOverride.entryOverrideType;
            }
            #endif
        }

        /// <summary>
        /// Returns the <see cref="EntryOverrideType"/> for the platform.
        /// </summary>
        /// <param name="tableReference">The table to use or <see langword="default"/> if it is not overriden.</param>
        /// <param name="tableEntryReference">The entry to use or <see langword="default"/> if it is not overriden.</param>
        /// <param name="platform">The platform to return the override for.</param>
        /// <returns>Returns the fields that should be overridden.</returns>
        public EntryOverrideType GetOverride(out TableReference tableReference, out TableEntryReference tableEntryReference, RuntimePlatform platform)
        {
            for (int i = 0; i < m_PlatformOverrides.Count; ++i)
            {
                if (m_PlatformOverrides[i].platform == platform)
                {
                    var po = m_PlatformOverrides[i];
                    tableReference = po.tableReference;
                    tableEntryReference = po.tableEntryReference;
                    return po.entryOverrideType;
                }
            }

            tableReference = default;
            tableEntryReference = default;
            return EntryOverrideType.None;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            #if !UNITY_EDITOR
            for (int i = 0; i < m_PlatformOverrides.Count; ++i)
            {
                if (m_PlatformOverrides[i].platform == Application.platform)
                {
                    m_PlayerPlatformOverride = m_PlatformOverrides[i];
                    return;
                }
            }
            #endif
        }
    }
}
