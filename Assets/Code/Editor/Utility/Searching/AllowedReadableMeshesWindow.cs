using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Searching {
    public class AllowedReadableMeshesWindow : AllowedReadableAssetsWindow<Mesh, ModelImporter> {
        [MenuItem("TG/Search/Find Meshes With ReadWrite Enabled")]
        static void ShowWindow() {
            GetWindow<AllowedReadableMeshesWindow>().Show();
        }

        protected override string GetAssetName() => "Mesh";

        protected override AllowedReadableAssetsSingleton<Mesh> GetAllowedReadableAssetsSO() {
            return AssetDatabaseUtils.GetSingletonScriptableObject<AllowedReadableMeshes>();
        }

        protected override bool IsReadable(ModelImporter assetImporter) {
            return assetImporter.isReadable;
        }

        protected override void SetIsReadable(ModelImporter assetImporter, bool isReadable) {
            assetImporter.isReadable = isReadable;
            assetImporter.SaveAndReimport();
        }
    }
}