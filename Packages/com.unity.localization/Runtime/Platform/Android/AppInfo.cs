using System;
using UnityEngine.Localization.Metadata;

namespace UnityEngine.Localization.Platform.Android
{
    /// <summary>
    /// Contains all values that can be localized in the Android Application.
    /// These values can be displayed when the App is not running, such as the application name that is displayed on the home screen.
    /// </summary>
    /// <example>
    /// This example shows how to configure the Android App Info.
    /// __Note__: You must complete this configuration before the application is built, otherwise, the changes will not be applied.
    /// <code source="../../../DocCodeSamples.Tests/AndroidAppInfoExample.cs"/>
    /// </example>
    [DisplayName("Android App Info", "Packages/com.unity.localization/Editor/Icons/Android/Android.png")]
    [Metadata(AllowedTypes = MetadataType.LocalizationSettings, AllowMultiple = false, MenuItem = "Android/App Info")]
    [Serializable]
    public class AppInfo : IMetadata
    {
        [Tooltip("The user-visible name for the bundle, used by Google Assistant and visible on the Android Home screen.\n")]
        [SerializeField] LocalizedString m_DisplayName = new LocalizedString();

        /// <summary>
        /// The user-visible name for the bundle, used by Google Assistant and visible on the Android Home screen.
        /// </summary>
        public LocalizedString DisplayName { get => m_DisplayName; set => m_DisplayName = value; }
    }
}
