using System;
using System.Collections.Generic;
using UnityEngine.Localization.Metadata;

namespace UnityEngine.Localization.Platform.Android
{
    /// <summary>
    /// Provides Android Adaptive icon configuration
    /// Please see [Android Documentation](https://developer.android.com/guide/practices/ui_guidelines/icon_design_adaptive) for more details on Adaptive Icon.
    /// </summary>
    [Serializable]
    public class AdaptiveIcon
    {
        [SerializeField] LocalizedTexture m_Background;
        [SerializeField] LocalizedTexture m_Foreground;

        /// <summary>
        /// Updates the background inner element of the Adaptive Icon for respective density qualifiers that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/guide/practices/ui_guidelines/icon_design_adaptive) for more details on Adaptive Icon.
        /// </summary>
        public LocalizedTexture Background { get => m_Background; set => m_Background = value; }

        /// <summary>
        /// Updates the foreground inner element of the Adaptive Icon for respective density qualifiers that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/guide/practices/ui_guidelines/icon_design_adaptive) for more details on Adaptive Icon.
        /// </summary>
        public LocalizedTexture Foreground { get => m_Foreground; set => m_Foreground = value; }
    }

    /// <summary>
    /// Contains values for configuring the Android Adaptive Icon.
    /// An Adaptive Launcher Icon represents the application icon on the device's Home Screen and in the launcher window on devices running Android 8 and above.
    /// Please see [Android Documentation](https://developer.android.com/guide/practices/ui_guidelines/icon_design_adaptive) for more details on Adaptive Icon.
    /// </summary>
    /// <example>
    /// This example shows how to configure the Android App Info.
    ///  __Note__: You must complete this configuration before the application is built, otherwise, the changes will not be applied.
    /// <code source="../../../DocCodeSamples.Tests/AndroidAppInfoExample.cs"/>
    /// </example>
    [DisplayName("Android Adaptive Icon Info")]
    [Metadata(AllowedTypes = MetadataType.LocalizationSettings, AllowMultiple = false, MenuItem = "Android/Adaptive Icon")]
    [Serializable]
    public class AdaptiveIconsInfo : IMetadata
    {
        [SerializeField] AdaptiveIcon m_Adaptive_idpi;
        [SerializeField] AdaptiveIcon m_Adaptive_mdpi;
        [SerializeField] AdaptiveIcon m_Adaptive_hdpi;
        [SerializeField] AdaptiveIcon m_Adaptive_xhdpi;
        [SerializeField] AdaptiveIcon m_Adaptive_xxhdpi;
        [SerializeField] AdaptiveIcon m_Adaptive_xxxhdpi;

        internal List<AdaptiveIcon> AdaptiveIcons = new List<AdaptiveIcon>();

        internal void RefreshAdaptiveIcons()
        {
            AdaptiveIcons.Clear();

            AdaptiveIcons.Add(m_Adaptive_idpi);
            AdaptiveIcons.Add(m_Adaptive_mdpi);
            AdaptiveIcons.Add(m_Adaptive_hdpi);
            AdaptiveIcons.Add(m_Adaptive_xhdpi);
            AdaptiveIcons.Add(m_Adaptive_xxhdpi);
            AdaptiveIcons.Add(m_Adaptive_xxxhdpi);
        }

        /// <summary>
        /// Contains resources for high-density (hdpi) screens (~240dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public AdaptiveIcon AdaptiveHdpi { get => m_Adaptive_hdpi; set => m_Adaptive_hdpi = value; }

        /// <summary>
        /// Contains resources for low-density (ldpi) screens (~120dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public AdaptiveIcon AdaptiveIdpi { get => m_Adaptive_idpi; set => m_Adaptive_idpi = value; }

        /// <summary>
        /// Contains resources for medium-density (mdpi) screens (~160dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public AdaptiveIcon AdaptiveMdpi { get => m_Adaptive_mdpi; set => m_Adaptive_mdpi = value; }

        /// <summary>
        /// Contains resources for extra-high-density (xhdpi) screens (~320dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public AdaptiveIcon AdaptiveXhdpi { get => m_Adaptive_xhdpi; set => m_Adaptive_xhdpi = value; }

        /// <summary>
        /// Contains resources for extra-extra-high-density (xxhdpi) screens (~480dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public AdaptiveIcon AdaptiveXXHdpi { get => m_Adaptive_xxhdpi; set => m_Adaptive_xxhdpi = value; }

        /// <summary>
        /// Contains resources for extra-extra-extra-high-density (xxxhdpi) uses (~640dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public AdaptiveIcon AdaptiveXXXHdpi { get => m_Adaptive_xxxhdpi; set => m_Adaptive_xxxhdpi = value; }
    }

    /// <summary>
    /// Contains values for configuring the Android Round Icon.
    /// An Round Launcher Icon represents the application icon on the device's Home Screen and in the launcher window (if launcher supports round icons) on devices running Android 7.1 and above.
    /// Please see [Android Documentation](https://developer.android.com/about/versions/nougat/android-7.1.html#circular-icons) for more details on Round Icon.
    /// </summary>
    [DisplayName("Android Round Icon Info")]
    [Metadata(AllowedTypes = MetadataType.LocalizationSettings, AllowMultiple = false, MenuItem = "Android/Round Icon")]
    [Serializable]
    public class RoundIconsInfo : IMetadata
    {
        [SerializeField] LocalizedTexture m_Round_idpi;
        [SerializeField] LocalizedTexture m_Round_mdpi;
        [SerializeField] LocalizedTexture m_Round_hdpi;
        [SerializeField] LocalizedTexture m_Round_xhdpi;
        [SerializeField] LocalizedTexture m_Round_xxhdpi;
        [SerializeField] LocalizedTexture m_Round_xxxhdpi;

        internal List<LocalizedTexture> RoundIcons = new List<LocalizedTexture>();

        internal void RefreshRoundIcons()
        {
            RoundIcons.Clear();

            RoundIcons.Add(m_Round_idpi);
            RoundIcons.Add(m_Round_mdpi);
            RoundIcons.Add(m_Round_hdpi);
            RoundIcons.Add(m_Round_xhdpi);
            RoundIcons.Add(m_Round_xxhdpi);
            RoundIcons.Add(m_Round_xxxhdpi);
        }

        /// <summary>
        /// Contains resources for high-density (hdpi) screens (~240dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture RoundHdpi { get => m_Round_hdpi; set => m_Round_hdpi = value; }

        /// <summary>
        /// Contains resources for low-density (ldpi) screens (~120dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture RoundIdpi { get => m_Round_idpi; set => m_Round_idpi = value; }

        /// <summary>
        /// Contains resources for medium-density (mdpi) screens (~160dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture RoundMdpi { get => m_Round_mdpi; set => m_Round_mdpi = value; }

        /// <summary>
        /// Contains resources for extra-high-density (xhdpi) screens (~320dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture RoundXhdpi { get => m_Round_xhdpi; set => m_Round_xhdpi = value; }

        /// <summary>
        /// Contains resources for extra-extra-high-density (xxhdpi) screens (~480dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture RoundXXHdpi { get => m_Round_xxhdpi; set => m_Round_xxhdpi = value; }

        /// <summary>
        /// Contains resources for extra-extra-extra-high-density (xxxhdpi) uses (~640dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture RoundXXXHdpi { get => m_Round_xxxhdpi; set => m_Round_xxxhdpi = value; }
    }


    /// <summary>
    /// Contains values for configuring the Android Legacy Icon.
    /// An Legacy Launcher Icon represents the application icon on the device's Home Screen and in the launcher window on devices running Android 7.1 and earlier.
    /// Please see [Android Documentation](https://developer.android.com/about/versions/nougat/android-7.1.html) for more details.
    /// </summary>
    [DisplayName("Android Legacy Icon Info")]
    [Metadata(AllowedTypes = MetadataType.LocalizationSettings, AllowMultiple = false, MenuItem = "Android/Legacy Icon")]
    [Serializable]
    public class LegacyIconsInfo : IMetadata
    {
        [SerializeField] LocalizedTexture m_Legacy_idpi;
        [SerializeField] LocalizedTexture m_Legacy_mdpi;
        [SerializeField] LocalizedTexture m_Legacy_hdpi;
        [SerializeField] LocalizedTexture m_Legacy_xhdpi;
        [SerializeField] LocalizedTexture m_Legacy_xxhdpi;
        [SerializeField] LocalizedTexture m_Legacy_xxxhdpi;

        internal List<LocalizedTexture> LegacyIcons = new List<LocalizedTexture>();

        internal void RefreshLegacyIcons()
        {
            LegacyIcons.Clear();

            LegacyIcons.Add(m_Legacy_idpi);
            LegacyIcons.Add(m_Legacy_mdpi);
            LegacyIcons.Add(m_Legacy_hdpi);
            LegacyIcons.Add(m_Legacy_xhdpi);
            LegacyIcons.Add(m_Legacy_xxhdpi);
            LegacyIcons.Add(m_Legacy_xxxhdpi);
        }

        /// <summary>
        /// Contains resources for high-density (hdpi) screens (~240dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture LegacyHdpi { get => m_Legacy_hdpi; set => m_Legacy_hdpi = value; }

        /// <summary>
        /// Contains resources for low-density (ldpi) screens (~120dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture LegacyIdpi { get => m_Legacy_idpi; set => m_Legacy_idpi = value; }

        /// <summary>
        /// Contains resources for medium-density (mdpi) screens (~160dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture LegacyMdpi { get => m_Legacy_mdpi; set => m_Legacy_mdpi = value; }

        /// <summary>
        /// Contains resources for extra-high-density (xhdpi) screens (~320dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture LegacyXhdpi { get => m_Legacy_xhdpi; set => m_Legacy_xhdpi = value; }

        /// <summary>
        /// Contains resources for extra-extra-high-density (xxhdpi) screens (~480dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture LegacyXXHdpi { get => m_Legacy_xxhdpi; set => m_Legacy_xxhdpi = value; }

        /// <summary>
        /// Contains resources for extra-extra-extra-high-density (xxxhdpi) uses (~640dpi) that can be localized in the Android Application
        /// Please see [Android Documentation](https://developer.android.com/training/multiscreen/screendensities#TaskProvideAltBmp) for more details on density qualifiers.
        /// </summary>
        public LocalizedTexture LegacyXXXHdpi { get => m_Legacy_xxxhdpi; set => m_Legacy_xxxhdpi = value; }
    }
}
