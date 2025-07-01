namespace UnityEngine.Localization
{
    class AddressHelper
    {
        const char k_Separator = '_';

        const string k_AssetLabelPrefix = "Locale-";

        public static string GetTableAddress(string tableName, LocaleIdentifier localeId)
        {
            return $"{tableName}{k_Separator}{localeId.Code}";
        }

        public static string GetSharedTableAddress(string tableName)
        {
            return $"{tableName} Shared Data";
        }

        public static string FormatAssetLabel(LocaleIdentifier localeIdentifier) => k_AssetLabelPrefix + localeIdentifier.Code;

        public static bool IsLocaleLabel(string label) => label.StartsWith(k_AssetLabelPrefix, System.StringComparison.InvariantCulture);

        public static LocaleIdentifier LocaleLabelToId(string label)
        {
            LocaleIdentifier id = default;
            Debug.Assert(TryGetLocaleLabelToId(label, out id), $"Expected label {label} to be a Locale label.");
            return id;
        }

        public static bool TryGetLocaleLabelToId(string label, out LocaleIdentifier localeId)
        {
            if (!IsLocaleLabel(label))
            {
                localeId = default;
                return false;
            }

            localeId = label.Substring(k_AssetLabelPrefix.Length, label.Length - k_AssetLabelPrefix.Length);
            return true;
        }
    }
}
