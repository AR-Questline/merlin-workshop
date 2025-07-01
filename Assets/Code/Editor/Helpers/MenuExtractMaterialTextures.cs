using System.IO;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG
{
    public class MenuExtractMaterialTextures {

        [MenuItem("Assets/TG/Extract textures from material", true)]
        public static bool CanExtractFromMaterial() {
            return Selection.activeObject is Material;
        }

        [MenuItem("Assets/TG/Extract textures from material")]
        public static void ExtractFromMaterial() {
            Material mat = Selection.activeObject as Material;
            string basePath = AssetDatabase.GetAssetPath(mat);
            ExportTexture(basePath, "albedo.png", mat.GetTexture("_MainTex"));
            ExportTexture(basePath, "metallic.png", mat.GetTexture("_MetallicGlossMap"));
            ExportTexture(basePath, "normal.png", mat.GetTexture("_BumpMap"));
        }

        static void ExportTexture(string baseName, string textureName, Texture texture) {
            if (texture == null) return;
            Texture2D t2d = (Texture2D) texture;
            Texture2D extracted = new Texture2D(t2d.width, t2d.height, TextureFormat.ARGB32, false);
            extracted.SetPixels(t2d.GetPixels());
            string path = Path.Combine(Path.GetDirectoryName(baseName) ?? "Assets/", textureName);
            File.WriteAllBytes(path, extracted.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
    }
}
