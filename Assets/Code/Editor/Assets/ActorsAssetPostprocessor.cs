using System.Linq;
using Awaken.TG.Main.Stories.Actors;
using UnityEditor;

namespace Awaken.TG.Editor.Assets {
    public class ActorsAssetPostprocessor : AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            // Refresh the actor cache when its prefab is saved (Unity reimports assets on save)
            if (importedAssets.Any(assetPath => assetPath == "Assets/Data/Settings/Actors.prefab")) {
                ActorsRegister.Editor_RefreshActorSpecCache();
            }
        }
    }
}