#if UNITY_IOS || UNITY_IPHONE
using System;
using System.IO;
using System.Text;
using UnityEditor.iOS.Xcode;
using UnityEditor.Localization.Platform.Utility;
using UnityEngine.Localization;
using UnityEngine.Localization.Platform.iOS;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Localization.Platform.iOS
{
    public static class Player
    {
        const string k_InfoFile = "InfoPlist.strings";

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
        /// Updates the Xcode project file with localized values using the <see cref="AppInfo"/> from <see cref="LocalizationSettings.Metadata"/>.
        /// </summary>
        /// <param name="projectDirectory">The root project directory to be updated. This is where the iOS player was built to.</param>
        /// <param name="appInfo"></param>
        public static void AddLocalizationToXcodeProject(string projectDirectory)
        {
            var appInfo = LocalizationSettings.Metadata.GetMetadata<AppInfo>();
            if (appInfo == null)
                return;
            AddLocalizationToXcodeProject(projectDirectory, appInfo);
        }

        /// <summary>
        /// Updates the Xcode project file with localized values using <see cref="AppInfo"/>.
        /// </summary>
        /// <param name="projectDirectory">The root project directory to be updated. This is where the iOS player was built to.</param>
        /// <param name="appInfo">Contains the localized values for the App.</param>
        public static void AddLocalizationToXcodeProject(string projectDirectory, AppInfo appInfo)
        {
            if (appInfo == null)
                throw new ArgumentNullException(nameof(appInfo));

            var pbxPath = PBXProject.GetPBXProjectPath(projectDirectory);
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);
            project.ClearKnownRegions(); // Remove the deprecated regions that get added automatically.

            var plistDocument = new PlistDocument();
            var plistPath = Path.Combine(projectDirectory, "Info.plist");
            plistDocument.ReadFromFile(plistPath);

            // Default language
            // How iOS Determines the Language For Your App - https://developer.apple.com/library/archive/qa/qa1828/_index.html
            var developmentRegion = string.IsNullOrEmpty(LocalizationSettings.Instance.m_ProjectLocaleIdentifier.Code) ? LocalizationEditorSettings.GetLocales() ? [0]?.Identifier : LocalizationSettings.Instance.m_ProjectLocaleIdentifier;
            if (developmentRegion.HasValue)
                project.SetDevelopmentRegion(developmentRegion.Value.Code);
            plistDocument.root.SetString("CFBundleDevelopmentRegion", "$(DEVELOPMENT_LANGUAGE)");

            // Inclusion of this key improves performance associated with displaying localized application names.
            plistDocument.root.SetBoolean("LSHasLocalizedDisplayName", true);

            var bundleLanguages = plistDocument.root.CreateArray("CFBundleLocalizations");
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var code = locale.Identifier.Code.Replace("-", "_");
                project.AddKnownRegion(code);
                bundleLanguages.AddString(code);

                var localeDir = code + ".lproj";
                var dir = Path.Combine(projectDirectory, localeDir);
                Directory.CreateDirectory(dir);

                var filePath = Path.Combine(dir, k_InfoFile);
                var relativePath = Path.Combine(localeDir, k_InfoFile);

                GenerateLocalizedInfoPlistFile(locale, appInfo, plistDocument, filePath);
                project.AddLocaleVariantFile(k_InfoFile, code, relativePath);
            }

            // Defaults
            var projectLocale = LocalizationSettings.ProjectLocale;
            if (projectLocale != null)
            {
                WriteDefaultLocalizedValue("CFBundleName", projectLocale, appInfo.ShortName, plistDocument);
                WriteDefaultLocalizedValue("CFBundleDisplayName", projectLocale, appInfo.DisplayName, plistDocument);
                WriteDefaultLocalizedValue("NSCameraUsageDescription", projectLocale, appInfo.CameraUsageDescription, plistDocument);
                WriteDefaultLocalizedValue("NSMicrophoneUsageDescription", projectLocale, appInfo.MicrophoneUsageDescription, plistDocument);
                WriteDefaultLocalizedValue("NSLocationWhenInUseUsageDescription", projectLocale, appInfo.LocationUsageDescription, plistDocument);
                WriteDefaultLocalizedValue("NSUserTrackingUsageDescription", projectLocale, appInfo.UserTrackingUsageDescription, plistDocument);
            }
            else
            {
                Debug.LogWarning("Project Locale was not configured. Can not set default IOS localized values. Configure the project locale through the Localization Settings.");
            }

            plistDocument.WriteToFile(plistPath);
            project.WriteToFile(pbxPath);
        }

        static void GenerateLocalizedInfoPlistFile(Locale locale, AppInfo appInfo, PlistDocument plistDocument, string filePath)
        {
            using (var stream = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                stream.Write(
                    "/*\n" +
                    $"\t{k_InfoFile}\n" +
                    $"\tThis file was auto-generated by {LocalizationPackageInfo.name}\n" +
                    $"\tVersion {LocalizationPackageInfo.version}\n" +
                    $"\tChanges to this file may cause incorrect behavior and will be lost if the project is rebuilt.\n" +
                    $"*/\n\n");

                WriteLocalizedValue("CFBundleName", stream, locale, appInfo.ShortName, plistDocument);
                WriteLocalizedValue("CFBundleDisplayName", stream, locale, appInfo.DisplayName, plistDocument);
                WriteLocalizedValue("NSCameraUsageDescription", stream, locale, appInfo.CameraUsageDescription, plistDocument);
                WriteLocalizedValue("NSMicrophoneUsageDescription", stream, locale, appInfo.MicrophoneUsageDescription, plistDocument);
                WriteLocalizedValue("NSLocationWhenInUseUsageDescription", stream, locale, appInfo.LocationUsageDescription, plistDocument);
                WriteLocalizedValue("NSUserTrackingUsageDescription", stream, locale, appInfo.UserTrackingUsageDescription, plistDocument);
            }
        }

        static void WriteLocalizedValue(string valueName, StreamWriter stream, Locale locale, LocalizedString localizedString, PlistDocument plistDocument)
        {
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
                    WriteLocalizedValue(valueName, stream, fallBack, localizedString, plistDocument);
                    return;
                }

                Debug.LogWarning($"{valueName}: Could not find a localized value for {locale} from {localizedString}");
                return;
            }

            Debug.Assert(!entry.IsSmart, $"Localized App Values ({valueName}) do not support Smart Strings - {localizedString}");
            stream.WriteLine($"\"{valueName}\" = \"{entry.LocalizedValue}\";");
        }

        static void WriteDefaultLocalizedValue(string valueName, Locale locale, LocalizedString localizedString, PlistDocument plistDocument)
        {
            if (localizedString.IsEmpty)
                return;

            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(localizedString.TableReference);
            var table = tableCollection?.GetTable(locale.Identifier) as StringTable;
            var entry = table?.GetEntryFromReference(localizedString.TableEntryReference);
            plistDocument.root.SetString(valueName, entry?.LocalizedValue);
        }
    }
}
#endif
