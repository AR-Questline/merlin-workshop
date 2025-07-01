namespace UnityEngine.Localization
{
    /// <summary>
    /// Utility class to handle addresses that may contain subassets in the form <c>Guid[SubAssetName]</c>.
    /// E.G <c>3b617f6c0e3720a4fbc476ee33e2041b[Cylinder]</c>.
    /// </summary>
    static class AssetAddress
    {
        const string k_SubAssetEntryStartBracket = "[";
        const string k_SubAssetEntryEndBracket = "]";

        /// <summary>
        /// Does the address contain a sub-asset?
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsSubAsset(string address) => address != null && address.EndsWith(k_SubAssetEntryEndBracket);

        /// <summary>
        /// Extracts the Guid from the address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetGuid(string address)
        {
            if (!IsSubAsset(address))
                return address;

            var startIdx = address.IndexOf(k_SubAssetEntryStartBracket);
            return address.Substring(0, startIdx);
        }

        /// <summary>
        /// Extracst the sub-asset name from the address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>The extracted name; otherwise <see langword="null"/> if one does not exist.</returns>
        public static string GetSubAssetName(string address)
        {
            if (!IsSubAsset(address))
                return null;

            var startIdx = address.IndexOf(k_SubAssetEntryStartBracket);
            var len = address.Length - startIdx - 2;
            return address.Substring(startIdx + 1, len);
        }

        /// <summary>
        /// Returns the Address in the expected Addessables format.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="subAssetName"></param>
        /// <returns></returns>
        public static string FormatAddress(string guid, string subAssetName) => $"{guid}[{subAssetName}]";
    }
}
