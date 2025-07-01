#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Awaken.Utility.Files {
    public static class EditorAssetUtil {
        public static Texture2D Create(Texture2D texture, string directoryInAssetDatabase, string filename, TextureEncoding encoding = TextureEncoding.Png) {
            if (directoryInAssetDatabase.StartsWith("Assets/") || directoryInAssetDatabase.StartsWith("Assets\\")) {
                directoryInAssetDatabase = directoryInAssetDatabase[7..];
            }
            var (bytes, extension) = encoding switch {
                TextureEncoding.Png => (texture.EncodeToPNG(), "png"),
                TextureEncoding.Jpg => (texture.EncodeToJPG(), "jpg"),
                TextureEncoding.Tga => (texture.EncodeToTGA(), "tga"),
                _ => throw new ArgumentException("Cannot save texture due to unsupported encoding")
            };
            var absolutePath = $"{Application.dataPath}/{directoryInAssetDatabase}/{filename}.{extension}";
            var assetDatabasePath = $"Assets/{directoryInAssetDatabase}/{filename}.{extension}";
            File.WriteAllBytes(absolutePath, bytes);
            AssetDatabase.ImportAsset(assetDatabasePath);
            var importer = (TextureImporter) AssetImporter.GetAtPath(assetDatabasePath);
            importer.sRGBTexture = texture.isDataSRGB;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetDatabasePath);
        }
        
        public enum TextureEncoding : byte {
            Png,
            Jpg,
            Tga,
        }
    }
}
#endif