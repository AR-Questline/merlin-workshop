using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Searching {
    public class AllowedReadableTexturesWindow : AllowedReadableAssetsWindow<Texture, TextureImporter> {
        [MenuItem("TG/Search/Find Textures With ReadWrite Enabled")]
        static void ShowWindow() {
            GetWindow<AllowedReadableTexturesWindow>().Show();
        }

        protected override string GetAssetName() => "Texture";

        protected override AllowedReadableAssetsSingleton<Texture> GetAllowedReadableAssetsSO() {
            return AssetDatabaseUtils.GetSingletonScriptableObject<AllowedReadableTextures>();
        }

        protected override bool IsReadable(TextureImporter assetImporter) {
            return assetImporter.isReadable;
        }

        protected override void SetIsReadable(TextureImporter assetImporter, bool isReadable) {
            assetImporter.isReadable = isReadable;
            assetImporter.SaveAndReimport();
        }
    }
}