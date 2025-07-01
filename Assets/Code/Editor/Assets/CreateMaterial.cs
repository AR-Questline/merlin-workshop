using UnityEditor;

namespace Awaken.TG.Editor.Assets {
    public static class CreateMaterial {
        [MenuItem("Assets/TG/Create material")]
        static void OpenWindow() {
            var textures = MaterialCreation.GetTexturesInSelected();
            foreach (var (name, texturePair) in textures) {
                MaterialCreation.CreateMaterialFromTextures(name, texturePair);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
