#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Contains Editor only localization Settings.
    /// </summary>
    [FilePath("ProjectSettings/LocalizationSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocalizationProjectSettings : ScriptableSingleton<LocalizationProjectSettings>
    {
        [SerializeField] LocalizedStringTable m_StringTable;
        [SerializeField] LocalizedAssetTable m_AssetTable;
        [SerializeField] bool m_TrackingChanges;

        /// <summary>
        /// The <see cref="StringTableCollection"/> that new string properties are added to.
        /// If <see langword="null"/>, the values will be stored locally inside a <see cref="UnityEngine.Localization.PropertyVariants.TrackedProperties.StringTrackedProperty"/>.
        /// </summary>
        public static LocalizedStringTable NewStringTable
        {
            get => instance.m_StringTable;
            set
            {
                instance.m_StringTable = value;
                instance.Save();
            }
        }

        /// <summary>
        /// The <see cref="AssetTableCollection"/> that new asset properties will use.
        /// If <see langword="null"/>, the values will be stored locally inside a <see cref="UnityEngine.Localization.PropertyVariants.TrackedProperties.UnityObjectProperty"/>.
        /// </summary>
        public static LocalizedAssetTable NewAssetTable
        {
            get => instance.m_AssetTable;
            set
            {
                instance.m_AssetTable = value;
                instance.Save();
            }
        }

        /// <summary>
        /// When enabled, any changes you make in a scene are recorded against the current <see cref="UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale"/>.
        /// </summary>
        public static bool TrackChanges
        {
            get => instance.m_TrackingChanges;
            set
            {
                instance.m_TrackingChanges = value;
                instance.Save();
            }
        }

        internal void Save() => instance.Save(true);
    }
}

#endif
