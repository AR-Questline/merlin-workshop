using System;
using UnityEngine.Localization.Metadata;

namespace UnityEngine.Localization.Platform.iOS
{
    /// <summary>
    /// Contains all values that can be localized in the iOS Application.
    /// These are values that can be displayed when the App is not running such as the application name displayed on the home screen.
    /// </summary>
    /// <example>
    /// This example shows how to configure the iOS AppInfo.
    /// __Note__: You must complete this configuration before the application is built, otherwise, the changes will not be applied.
    /// <code source="../../../DocCodeSamples.Tests/IosAppInfoExample.cs"/>
    /// </example>
    [DisplayName("iOS App Info")]
    [Metadata(AllowedTypes = MetadataType.LocalizationSettings, AllowMultiple = false, MenuItem = "iOS/App Info")]
    [Serializable]
    public class AppInfo : IMetadata
    {
        [Tooltip("The user-visible name for the bundle, used by Siri and visible on the iOS Home screen.\n" +
            "This name can contain up to 15 characters.\n" +
            "CFBundleName field in xcode projects info.plist file.")]
        [SerializeField] LocalizedString m_ShortName = new LocalizedString();

        [Tooltip("The user-visible name for the bundle, used by Siri and visible on the iOS Home screen.\n" +
            "Use this key if you want a product name that's longer than Bundle Name.\n" +
            "CFBundleDisplayName field in xcode projects info.plist file.")]
        [SerializeField] LocalizedString m_DisplayName = new LocalizedString();

        [Tooltip("A message that tells the user why the app is requesting access to the device’s camera.\n" +
            "NSCameraUsageDescription field in xcode projects info.plist file.")]
        [SerializeField] LocalizedString m_CameraUsageDescription = new LocalizedString();

        [Tooltip("A message that tells the user why the app is requesting access to the device’s microphone.\n" +
            "NSMicrophoneUsageDescription field in xcode projects info.plist file.")]
        [SerializeField] LocalizedString m_MicrophoneUsageDescription = new LocalizedString();

        [Tooltip("A message that tells the user why the app is requesting access to the user’s location " +
            "information while the app is running in the foreground.\n" +
            "NSLocationWhenInUseUsageDescription field in xcode projects info.plist file.")]
        [SerializeField] LocalizedString m_LocationUsageDescription = new LocalizedString();

        [Tooltip("A message that informs the user why an app is requesting permission to use data for " +
            "tracking the user or the device.\n" +
            "NSUserTrackingUsageDescription field in xcode projects info.plist file.")]
        [SerializeField] LocalizedString m_UserTrackingUsageDescription = new LocalizedString();

        /// <summary>
        /// The user-visible name for the bundle, used by Siri and visible on the iOS Home screen.
        /// This name can contain up to 15 characters.
        /// CFBundleName field in xcode projects info.plist file.
        /// </summary>
        public LocalizedString ShortName { get => m_ShortName; set => m_ShortName = value; }

        /// <summary>
        /// The user-visible name for the bundle, used by Siri and visible on the iOS Home screen.
        /// Use this key if you want a product name that's longer than <see cref="ShortName"/>.
        /// CFBundleDisplayName field in xcode projects info.plist file.
        /// </summary>
        public LocalizedString DisplayName { get => m_DisplayName; set => m_DisplayName = value; }

        /// <summary>
        /// A message that tells the user why the app is requesting access to the device’s camera.
        /// NSCameraUsageDescription field in xcode projects info.plist file
        /// </summary>
        public LocalizedString CameraUsageDescription { get => m_CameraUsageDescription; set => m_CameraUsageDescription = value; }

        /// <summary>
        /// A message that tells the user why the app is requesting access to the device’s microphone.
        /// NSMicrophoneUsageDescription field in xcode projects info.plist file.
        /// </summary>
        public LocalizedString MicrophoneUsageDescription { get => m_MicrophoneUsageDescription; set => m_MicrophoneUsageDescription = value; }

        /// <summary>
        /// A message that tells the user why the app is requesting access to the user’s location information
        /// while the app is running in the foreground.
        /// NSLocationWhenInUseUsageDescription field in xcode projects info.plist file.
        /// </summary>
        public LocalizedString LocationUsageDescription { get => m_LocationUsageDescription; set => m_LocationUsageDescription = value; }

        /// <summary>
        /// A message that informs the user why an app is requesting permission to use data for tracking the
        /// user or the device.
        /// NSUserTrackingUsageDescription field in xcode projects info.plist file.
        /// </summary>
        public LocalizedString UserTrackingUsageDescription { get => m_UserTrackingUsageDescription; set => m_UserTrackingUsageDescription = value; }
    }
}
