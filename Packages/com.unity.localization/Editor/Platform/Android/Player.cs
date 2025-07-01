#if UNITY_ANDROID || PACKAGE_DOCS_GENERATION
using System;
using System.IO;
using System.Text;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization.Platform.Utility;
using UnityEngine.Localization.Platform.Android;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.Localization.Platform.Android
{
    enum DPI_Info
    {
        IDPI,
        MDPI,
        HDPI,
        XHDPI,
        XXHDPI,
        XXXHDPI
    }

    public static class Player
    {
        const string k_InfoFile = "strings.xml";
        const string k_LegacyIconName = "app_icon.png";
        const string k_RoundIconName = "app_icon_round.png";
        const string k_AdaptiveIconBackgroundName = "ic_launcher_background.png";
        const string k_AdaptiveIconForegroundName = "ic_launcher_foreground.png";
        const string k_AdaptiveIcon_AppIconInfo = "app_icon.xml";
        const string k_AdaptiveIcon_AppRoundIconInfo = "app_icon_round.xml";
        const string k_IconsCacheFolderPath = "/../Library/com.unity.localization/Android/";

        static PackageManager.PackageInfo s_PackageInfo;
        static PackageManager.PackageInfo LocalizationPackageInfo
        {
            get
            {
                if (s_PackageInfo == null)
                {
                    s_PackageInfo = PackageManager.PackageInfo.FindForAssembly(typeof(LocalizationSettings).Assembly);
                }
                return s_PackageInfo;
            }
        }

        /// <summary>
        /// Updates the Gradle project 'strings.xml' file with localized values using the <see cref="AppInfo"/> from <see cref="LocalizationSettings.Metadata"/>.
        /// </summary>
        /// <param name="projectDirectory">The root project directory to be updated. This is where the Android player was built to.</param>
        public static void AddLocalizationToAndroidGradleProject(string projectDirectory)
        {
            var appInfo = LocalizationSettings.Metadata.GetMetadata<AppInfo>();
            var roundIconInfo = LocalizationSettings.Metadata.GetMetadata<RoundIconsInfo>();
            var legacyIconInfo = LocalizationSettings.Metadata.GetMetadata<LegacyIconsInfo>();
            var adaptiveIconInfo = LocalizationSettings.Metadata.GetMetadata<AdaptiveIconsInfo>();

            if (appInfo == null)
            {
                Debug.LogWarning("Android App Info has not been configured. Please add and configure `Android App Info` metadata to the Localization Settings in order to correctly support Localization on Android.");
                return;
            }

            AddLocalizationToAndroidGradleProject(projectDirectory, appInfo, roundIconInfo, legacyIconInfo, adaptiveIconInfo);
        }

        /// <summary>
        /// Updates the Android Gradle project file with localized values using <see cref="AppInfo"/>.
        /// </summary>
        /// <param name="projectDirectory">The root project directory to be updated. This is where the Android player was built to.</param>
        /// <param name="appInfo">Contains the localized values for the App.</param>
        /// <param name="roundIconsInfo">Contains the localized values for Android Round Icon. Refer Android documentation for more details : https://developer.android.com/about/versions/nougat/android-7.1.html#circular-icons</param>
        /// <param name="legacyIconsInfo">Contains the localized values for Android Legacy Icon.</param>
        /// <param name="adaptiveIconsInfo">Contains the localized values for Android Adaptive Icon. . Refer Android documentation for more details : https://developer.android.com/guide/practices/ui_guidelines/icon_design_adaptive</param>
        public static void AddLocalizationToAndroidGradleProject(string projectDirectory, AppInfo appInfo, RoundIconsInfo roundIconsInfo = null, LegacyIconsInfo legacyIconsInfo = null, AdaptiveIconsInfo adaptiveIconsInfo = null)
        {
            if (appInfo == null)
                throw new ArgumentNullException(nameof(appInfo));

            var project = new GradleProjectSettings();
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var localeIdentifier = GenerateAndroidLanguageCode(locale.Identifier);
                GenerateLocalizedXmlFile("App Name", Path.Combine(Directory.CreateDirectory(Path.Combine(project.GetResFolderPath(projectDirectory), "values-b+" + localeIdentifier)).FullName, k_InfoFile), locale, appInfo);

                //Generate icons
                var folderNames = new List<string>
                {
                    $"mipmap-{localeIdentifier}-ldpi",
                    $"mipmap-{localeIdentifier}-mdpi",
                    $"mipmap-{localeIdentifier}-hdpi",
                    $"mipmap-{localeIdentifier}-xhdpi",
                    $"mipmap-{localeIdentifier}-xxhdpi",
                    $"mipmap-{localeIdentifier}-xxxhdpi"
                };
                var adaptiveIconFolderName = $"mipmap-{localeIdentifier}-anydpi-v26";


                if (roundIconsInfo != null || legacyIconsInfo != null)
                    GenerateIconDirectory(folderNames, project.GetResFolderPath(projectDirectory), locale);
                if (adaptiveIconsInfo != null)
                    GenerateIconDirectory(adaptiveIconFolderName, project.GetResFolderPath(projectDirectory), locale);

                if (roundIconsInfo != null)
                    GenerateRoundIcons(folderNames, project.GetResFolderPath(projectDirectory), locale, roundIconsInfo);

                if (legacyIconsInfo != null)
                    GenerateLegacyIcons(folderNames, project.GetResFolderPath(projectDirectory), locale, legacyIconsInfo);

                if (adaptiveIconsInfo != null)
                    GenerateAdaptiveIcons(folderNames, project.GetResFolderPath(projectDirectory), locale, adaptiveIconsInfo, adaptiveIconFolderName);
            }

            var androidManifest = new AndroidManifest(project.GetManifestPath(projectDirectory));
            androidManifest.SetAtrribute("label", project.LabelName);

            if (adaptiveIconsInfo != null || legacyIconsInfo != null || roundIconsInfo != null)
            {
                androidManifest.SetAtrribute("icon", project.IconLabelName);
                androidManifest.SetAtrribute("roundIcon", project.RoundIconLabelName);
            }

            androidManifest.SaveIfModified();
        }

        internal static string GenerateAndroidLanguageCode(LocaleIdentifier localeIdentifier)
        {
            // When we use System Language as Locale Source Chinese (Simplified) code is represented as (zh-hans) and Chinese (Traditional) code is represented as (zh-hant).
            // But Android Localization is case-sensitive and ony supports Chinese (Simplified) code as (zh-Hans) and Chinese (Traditional) code as (zh-Hant).
            // https://developer.android.com/reference/java/util/Locale.LanguageRange
            localeIdentifier = localeIdentifier.Code.Contains("hans") ? localeIdentifier.Code.Replace("hans", "Hans") : localeIdentifier.Code.Contains("hant") ? localeIdentifier.Code.Replace("hant", "Hant") : localeIdentifier;
            var code = localeIdentifier.Code;

            var IsSpecialLocaleIdentifier = code.Contains("Hans") || code.Contains("Hant") || code.Contains("Latn") || code.Contains("Cyrl") || code.Contains("Arab") || code.Contains("valencia");

            // The language is defined by a two-letter ISO 639-1 language code, optionally followed by a two letter ISO 3166-1-alpha-2 region code (preceded by lowercase r).
            // The codes are not case-sensitive; the r prefix is used to distinguish the region portion. You cannot specify a region alone.
            // https://developer.android.com/guide/topics/resources/providing-resources
            localeIdentifier = code.Contains("-") ? IsSpecialLocaleIdentifier ? code.Replace("-", "+") : code.Replace("-", "-r") : localeIdentifier;

            return localeIdentifier.Code;
        }

        static void GenerateIconDirectory(List<string> folderNames, string path, Locale locale)
        {
            foreach (var name in folderNames)
            {
                GenerateIconDirectory(name, path, locale);
            }
        }

        static void GenerateIconDirectory(string name, string path, Locale locale)
        {
            if (!Directory.Exists(Path.Combine(path, name)))
                Directory.CreateDirectory(Path.Combine(path, name));
        }

        static void GenerateRoundIcons(List<string> folderNames, string path, Locale locale, RoundIconsInfo iconInfo)
        {
            // We get the folder path of the respective density qualifier by combining the android `res' folder path and
            // the localized folder name from the folderNames list, for exmaple "mipmap-ar-hdpi" which is icon folder for arabic Locale
            // The k_RoundIconName variable contains the round icon name which will be used as file name for saving the respective LocalizedTexture to a ".png" file.
            iconInfo.RefreshRoundIcons();
            foreach (var foldername in folderNames.Select((value, index) => (value, index)))
            {
                GenerateLocalizedIcon(Path.Combine(Path.Combine(path, foldername.value), k_RoundIconName), locale, iconInfo.RoundIcons[foldername.index], (DPI_Info)foldername.index);
            }
        }

        static void GenerateLegacyIcons(List<string> folderNames, string path, Locale locale, LegacyIconsInfo iconInfo)
        {
            // We get the folder path of the respective density qualifier by combining the android `res' folder path and
            // the localized folder name from the folderNames list, for exmaple "mipmap-ar-hdpi" which is icon folder for arabic Locale
            // The k_LegacyIconName variable contains the round icon name which will be used as file name for saving the respective LocalizedTexture to a ".png" file.
            iconInfo.RefreshLegacyIcons();
            foreach (var foldername in folderNames.Select((value, index) => (value, index)))
            {
                GenerateLocalizedIcon(Path.Combine(Path.Combine(path, foldername.value), k_LegacyIconName), locale, iconInfo.LegacyIcons[foldername.index], (DPI_Info)foldername.index);
            }
        }

        static void GenerateAdaptiveIcons(List<string> folderNames, string path, Locale locale, AdaptiveIconsInfo iconInfo, string adaptiveIconXMLFolderName)
        {
            //Adaptive Background Icons
            // We get the folder path of the respective density qualifier by combining the android `res' folder path and
            // the localized folder name from the folderNames list, for exmaple "mipmap-ar-hdpi" which is icon folder for arabic Locale
            // The k_AdaptiveIconBackgroundName variable contains the name for background icon which we use it for our Adaptive icon,
            // which will be used as file name for saving the respective LocalizedTexture to a ".png" file.
            iconInfo.RefreshAdaptiveIcons();
            foreach (var foldername in folderNames.Select((value, index) => (value, index)))
            {
                GenerateLocalizedIcon(Path.Combine(Path.Combine(path, foldername.value), k_AdaptiveIconBackgroundName), locale, iconInfo.AdaptiveIcons[foldername.index].Background, (DPI_Info)foldername.index, true);
            }

            //Adaptive Foreground Icons
            // We get the folder path of the respective density qualifier by combining the android `res' folder path and
            // the localized folder name from the folderNames list, for exmaple "mipmap-ar-hdpi" which is icon folder for arabic Locale
            // The k_AdaptiveIconForegroundName variable contains the name for foreground icon which we use it for our Adaptive icon.
            // which will be used as file name for saving the respective LocalizedTexture to a ".png" file.
            foreach (var foldername in folderNames.Select((value, index) => (value, index)))
            {
                GenerateLocalizedIcon(Path.Combine(Path.Combine(path, foldername.value), k_AdaptiveIconForegroundName), locale, iconInfo.AdaptiveIcons[foldername.index].Foreground, (DPI_Info)foldername.index, true);
            }

            WriteAdaptiveIconsXML(Path.Combine(Path.Combine(path, adaptiveIconXMLFolderName), k_AdaptiveIcon_AppIconInfo));
            WriteAdaptiveIconsXML(Path.Combine(Path.Combine(path, adaptiveIconXMLFolderName), k_AdaptiveIcon_AppRoundIconInfo));
        }

        static void GenerateLocalizedIcon(string filePath, Locale locale, LocalizedTexture localizedTexture, DPI_Info resolution, bool IsAdaptiveIcon = false)
        {
            if (localizedTexture.IsEmpty)
                return;

            var tableCollection = LocalizationEditorSettings.GetAssetTableCollection(localizedTexture.TableReference);
            var table = tableCollection?.GetTable(locale.Identifier) as AssetTable;
            var entry = table?.GetEntryFromReference(localizedTexture.TableEntryReference);

            if (entry == null || string.IsNullOrWhiteSpace(entry.LocalizedValue))
            {
                var fallBack = FallbackLocaleHelper.GetLocaleFallback(locale);
                if (fallBack != null)
                {
                    GenerateLocalizedIcon(filePath, fallBack, localizedTexture, resolution, IsAdaptiveIcon);
                    return;
                }

                Debug.LogWarning($"Could not find a localized Icons for {locale} from {localizedTexture}");
                return;
            }
            var iconPath = AssetDatabase.GUIDToAssetPath(entry.Guid);
            if (string.IsNullOrEmpty(iconPath))
                return;

            if (File.Exists(filePath))
                File.Delete(filePath);

            var texture = AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D)) as Texture2D;
            var cachedDirectoryPath = Application.dataPath + k_IconsCacheFolderPath;
            cachedDirectoryPath = Directory.Exists(cachedDirectoryPath) ? cachedDirectoryPath : Directory.CreateDirectory(cachedDirectoryPath).FullName;
            var imageHash = texture.imageContentsHash.ToString();
            var cachedFilePath = $"{cachedDirectoryPath}{imageHash.ToString()}{resolution}";
            cachedFilePath = IsAdaptiveIcon ? cachedFilePath + "_Adaptive.png" : cachedFilePath + ".png";
            CreatePNG(cachedFilePath, filePath, texture, resolution, IsAdaptiveIcon);
        }

        static void CreatePNG(string filePath, string targetPath, Texture2D icon, DPI_Info dpi, bool IsAdaptiveIcon)
        {
            if (!File.Exists(filePath))
            {
                int width, height;
                GetResolution(dpi, out width, out height, IsAdaptiveIcon);
                var newResizedIcon = ResizeIcons(icon, width, height);
                var bytes = newResizedIcon.EncodeToPNG();

                //Saving the Icon as ".png" file to gradle res folder
                File.WriteAllBytes(filePath, bytes);
            }

            File.Copy(filePath, targetPath);
        }

        static Texture2D ResizeIcons(Texture2D source, int width, int height)
        {
            var scaledIcon = new Texture2D(width, height, TextureFormat.ARGB32, true);
            var iconPixels = scaledIcon.GetPixels(0);
            var incX = ((float)1 / source.width) * ((float)source.width / width);
            var incY = ((float)1 / source.height) * ((float)source.height / height);
            for (var pixel = 0; pixel < iconPixels.Length; pixel++)
            {
                iconPixels[pixel] = source.GetPixelBilinear(incX * ((float)pixel % width), incY * ((float)Mathf.Floor(pixel / width)));
            }
            scaledIcon.SetPixels(iconPixels, 0);
            scaledIcon.Apply();
            return scaledIcon;
        }

        static void GetResolution(DPI_Info dpi, out int width, out int height, bool IsAdaptiveIcon)
        {
            var resolution = IsAdaptiveIcon ? 9 : 12;
            switch (dpi)
            {
                case DPI_Info.MDPI:
                    if (IsAdaptiveIcon)
                        width = height = resolution * 12;
                    else
                        width = height = resolution * 4;
                    break;

                case DPI_Info.HDPI:
                    if (IsAdaptiveIcon)
                        width = height = resolution * 18;
                    else
                        width = height = resolution * 6;
                    break;

                case DPI_Info.XHDPI:
                    if (IsAdaptiveIcon)
                        width = height = resolution * 24;
                    else
                        width = height = resolution * 8;
                    break;

                case DPI_Info.XXHDPI:
                    if (IsAdaptiveIcon)
                        width = height = resolution * 36;
                    else
                        width = height = resolution * 12;
                    break;

                case DPI_Info.XXXHDPI:
                    if (IsAdaptiveIcon)
                        width = height = resolution * 48;
                    else
                        width = height = resolution * 16;
                    break;

                case DPI_Info.IDPI:
                default:
                    if (IsAdaptiveIcon)
                        width = height = resolution * 9;
                    else
                        width = height = resolution * 3;
                    break;
            }
        }

        static void WriteAdaptiveIconsXML(string filePath)
        {
            using (var stream = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                stream.WriteLine(
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>" +
                    "<!--" +
                    "\n" +
                    $"\tThis file was auto-generated by {LocalizationPackageInfo.name}\n" +
                    $"\tVersion {LocalizationPackageInfo.version}\n" +
                    $"\tChanges to this file may cause incorrect behavior and will be lost if the project is rebuilt.\n" +
                    $"-->" +
                    "\n" +
                    $@"<adaptive-icon xmlns:android=""http://schemas.android.com/apk/res/android"">
                            <background android:drawable=""@mipmap/ic_launcher_background""/>
                            <foreground android:drawable=""@mipmap/ic_launcher_foreground""/>
                        </adaptive-icon> ");
            }
        }

        static void GenerateLocalizedXmlFile(string valueName, string filePath, Locale locale, AppInfo appinfo)
        {
            var localizedString = appinfo.DisplayName;
            if (localizedString.IsEmpty)
                return;

            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(localizedString.TableReference);
            var table = tableCollection?.GetTable(locale.Identifier) as StringTable;
            var entry = table?.GetEntryFromReference(localizedString.TableEntryReference);

            if (entry == null || string.IsNullOrWhiteSpace(entry.LocalizedValue))
            {
                // Use fallback?
                var fallBack = FallbackLocaleHelper.GetLocaleFallback(locale);
                if (fallBack != null)
                {
                    GenerateLocalizedXmlFile(valueName, filePath, fallBack, appinfo);
                    return;
                }

                Debug.LogWarning($"{valueName}: Could not find a localized value for {locale} from {localizedString}");
                return;
            }

            // We are adding a back slash when the entry value contains an single quote, to prevent android build failures and show the display name with apostrophe ex: " J'adore ";
            // (?<!\\) - Negative Lookbehind to ignore any that already start with \\
            // (?<replace>') - match colon and place it into the replace variable
            var localizedValue = Regex.Replace(entry.LocalizedValue, @"(?<!\\)(?<replace>')", @"\'");
            Debug.Assert(!entry.IsSmart, $"Localized App Values ({valueName}) do not support Smart Strings - {localizedString}");

            using (var stream = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                stream.WriteLine(
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>" +
                    "<!--" +
                    "\n" +
                    $"\t{k_InfoFile}\n" +
                    $"\tThis file was auto-generated by {LocalizationPackageInfo.name}\n" +
                    $"\tVersion {LocalizationPackageInfo.version}\n" +
                    $"\tChanges to this file may cause incorrect behavior and will be lost if the project is rebuilt.\n" +
                    $"-->" +
                    "\n" +
                    $@"<resources>
                       <string name=""app_name""> {localizedValue} </string>
                       </resources>");
            }
        }
    }
}
#endif
