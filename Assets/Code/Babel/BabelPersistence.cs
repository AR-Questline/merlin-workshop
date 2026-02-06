using System.IO;
using Awaken.Utility.Archives;
using Awaken.Utility.Debugging;
using UnityEngine.Localization;

namespace Awaken.Babel {
    public static class BabelPersistence {
        public const string SubdirectoryName = "Languages";
        public const string ArchiveFileName = "languages.arch";

        public const string KeysDataName = "keys_data.blob";
        public const string KeysPositionsName = "keys_positions.blob";

        public const string GesturesDataName = "gestures_data.blob";
        public const string GesturesPositionsName = "gestures_positions.blob";

        public const string StringsFileName = "strings.blob";
        public const string PositionsFileName = "positions.blob";
        public const string SmartTagsFileName = "smart_tags.blob";

        public static readonly string BakingBaseDirectoryPath = Path.Combine("Library", SubdirectoryName);

        public static bool TryGetBasePathForMetaLoading(out string basePath) {
            basePath = BakingBaseDirectoryPath;
            var success = ArchiveUtils.TryMountAndAdjustPath("Babel", SubdirectoryName, ArchiveFileName, ref basePath);
            if (!success) {
                Log.Critical?.Error($"Babel meta archive not found");
                return false;
            }
            return true;
        }

        public static bool TryGetBasePathForLanguageLoading(in LocaleIdentifier locale, out string basePath) {
            basePath = BakingBaseDirectoryPath;
            var success = ArchiveUtils.TryMountAndAdjustPath("Babel", SubdirectoryName, ArchiveFileName, ref basePath);
            basePath = Path.Combine(basePath, locale.Code);
            if (!success) {
                Log.Critical?.Error($"Babel language {locale} archive not found");
                return false;
            }
            return true;
        }

        public static string GetKeysDataPath(string basePath) {
            return Path.Combine(basePath, KeysDataName);
        }

        public static string GetKeysPositionsPath(string basePath) {
            return Path.Combine(basePath, KeysPositionsName);
        }

        public static string GetStringsPath(string basePath) {
            return Path.Combine(basePath, StringsFileName);
        }

        public static string GetPositionsPath(string basePath) {
            return Path.Combine(basePath, PositionsFileName);
        }

        public static string GetSmartTagsPath(string basePath) {
            return Path.Combine(basePath, SmartTagsFileName);
        }

        public static string GetGesturesDataPath(string basePath) {
            return Path.Combine(basePath, GesturesDataName);
        }

        public static string GetGesturesPositionsPath(string basePath) {
            return Path.Combine(basePath, GesturesPositionsName);
        }
    }
}
