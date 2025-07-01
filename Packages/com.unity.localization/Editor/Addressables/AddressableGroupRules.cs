using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization.Addressables
{
    /// <summary>
    /// Provides support for configuring rule sets to determine the <see cref="AddressableAssetGroup"/>s that localized assets should be added to.
    /// </summary>
    /// <example>
    /// <para>This example places all English assets into a local group and all other languages into a remote group which could then be downloaded after the game is released.</para>
    /// <code source="../../DocCodeSamples.Tests/GroupResolverExample.cs"/>
    /// </example>
    public class AddressableGroupRules : ScriptableObject
    {
        const string k_ConfigName = "com.unity.localization.addressable-group-rules";

        [SerializeReference] GroupResolver m_LocaleResolver = new GroupResolver("Localization-Locales");
        [SerializeReference] GroupResolver m_StringTablesResolver = new GroupResolver("Localization-String-Tables-{LocaleName}", "Localization-Assets-Shared");
        [SerializeReference] GroupResolver m_AssetTablesResolver = new GroupResolver("Localization-Asset-Tables-{LocaleName}", "Localization-Assets-Shared");
        [SerializeReference] GroupResolver m_AssetResolver = new GroupResolver("Localization-Assets-{LocaleName}", "Localization-Assets-Shared");

        static AddressableGroupRules s_Instance;

        /// <summary>
        /// The active <see cref="AddressableGroupRules"/> that is being used by the project.
        /// </summary>
        public static AddressableGroupRules Instance
        {
            get
            {
                if (s_Instance == null && !EditorBuildSettings.TryGetConfigObject(k_ConfigName, out s_Instance))
                    s_Instance = CreateInstance<AddressableGroupRules>();
                return s_Instance;
            }
            set
            {
                if (s_Instance == value)
                    return;

                if (EditorUtility.IsPersistent(value))
                {
                    EditorBuildSettings.AddConfigObject(k_ConfigName, value, true);
                    Debug.Log("Localization Addressables Group Rules changed to " + AssetDatabase.GetAssetPath(value));
                }

                s_Instance = value;
            }
        }

        static void CreateAsset(Action<AddressableGroupRules> configureAction)
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Addressable Group Rules", "Localization Addressable Group Rules.asset", "asset", "");
            if (string.IsNullOrEmpty(path))
                return;

            var instance = CreateInstance<AddressableGroupRules>();

            configureAction?.Invoke(instance);
            AssetDatabase.CreateAsset(instance, path);
            Instance = instance;
        }

        [MenuItem("Assets/Create/Localization/Addressable Group Rules (Default)")]
        static void CreateDefault() => CreateAsset(null);

        [MenuItem("Assets/Create/Localization/Addressable Group Rules (Single Group)")]
        static void CreateSingleGroup()
        {
            CreateAsset(instance =>
            {
                instance.LocaleResolver = new GroupResolver("Localization", "Localization");
                instance.StringTablesResolver = new GroupResolver("Localization", "Localization");
                instance.AssetTablesResolver = new GroupResolver("Localization", "Localization");
                instance.AssetResolver = new GroupResolver("Localization", "Localization");
            });
        }

        [MenuItem("Assets/Create/Localization/Addressable Group Rules (Legacy)")]
        static void CreateLegacy()
        {
            CreateAsset(instance =>
            {
                instance.StringTablesResolver = new GroupResolver("Localization-StringTables", "Localization-Assets-Shared");
                instance.AssetTablesResolver = new GroupResolver("Localization-AssetTables", "Localization-Assets-Shared");
                instance.AssetResolver = new GroupResolver("Localization-Assets-{Identifier.Code}", "Localization-Assets-Shared");
            });
        }

        /// <summary>
        /// Controls which groups Locales are added to.
        /// </summary>
        public GroupResolver LocaleResolver
        {
            get => m_LocaleResolver;
            set
            {
                if (ReferenceEquals(m_LocaleResolver, value))
                    return;

                m_LocaleResolver = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Controls which groups <see cref="StringTable"/> and their <see cref="SharedTableData"/> are added to.
        /// </summary>
        public GroupResolver StringTablesResolver
        {
            get => m_StringTablesResolver;
            set
            {
                if (ReferenceEquals(m_StringTablesResolver, value))
                    return;

                m_StringTablesResolver = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Controls which groups <see cref="AssetTable"/> and their <see cref="SharedTableData"/> are added to.
        /// </summary>
        public GroupResolver AssetTablesResolver
        {
            get => m_AssetTablesResolver;
            set
            {
                if (ReferenceEquals(m_AssetTablesResolver, value))
                    return;

                m_AssetTablesResolver = value;
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Controls which groups assets that are part of 1 or more <see cref="AssetTable"/> will be added to.
        /// </summary>
        public GroupResolver AssetResolver
        {
            get => m_AssetResolver;
            set
            {
                if (ReferenceEquals(m_AssetResolver, value))
                    return;

                m_AssetResolver = value;
                EditorUtility.SetDirty(this);
            }
        }

        internal static AddressableAssetEntry AddLocaleToGroup(Locale asset, AddressableAssetSettings aaSettings, bool createUndo) => Instance.LocaleResolver.AddToGroup(asset, new[] { asset.Identifier }, aaSettings, createUndo);
        internal static AddressableAssetEntry AddStringTableSharedAsset(Object asset, AddressableAssetSettings aaSettings, bool createUndo) => Instance.StringTablesResolver.AddToGroup(asset, null, aaSettings, createUndo);
        internal static AddressableAssetEntry AddStringTableAsset(LocalizationTable table, AddressableAssetSettings aaSettings, bool createUndo) => Instance.StringTablesResolver.AddToGroup(table, new[] { table.LocaleIdentifier }, aaSettings, createUndo);
        internal static AddressableAssetEntry AddAssetTableAsset(LocalizationTable table, AddressableAssetSettings aaSettings, bool createUndo) => Instance.AssetTablesResolver.AddToGroup(table, new[] { table.LocaleIdentifier }, aaSettings, createUndo);
        internal static AddressableAssetEntry AddAssetTableSharedAsset(Object asset, AddressableAssetSettings aaSettings, bool createUndo) => Instance.AssetTablesResolver.AddToGroup(asset, null, aaSettings, createUndo);
        internal static AddressableAssetEntry AddAssetToGroup(Object asset, IList<LocaleIdentifier> locales, AddressableAssetSettings aaSettings, bool createUndo) => Instance.AssetResolver.AddToGroup(asset, locales, aaSettings, createUndo);
    }
}
