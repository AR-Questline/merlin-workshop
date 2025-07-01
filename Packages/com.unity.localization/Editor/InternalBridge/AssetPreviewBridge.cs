using UnityEngine;

namespace UnityEditor.Localization.Bridge
{
    static class AssetPreviewBridge
    {
        public static Texture2D GetAssetPreviewFromGUID(string guid) => AssetPreview.GetAssetPreviewFromGUID(guid);
    }
}
