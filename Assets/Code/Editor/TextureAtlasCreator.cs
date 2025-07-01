using System;
using System.Collections.Generic;
using System.IO;
using Awaken.Utility.Debugging;
using Awaken.Utility.Files;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

namespace Awaken.TG.Editor {
    public class TextureAtlasCreator : EditorWindow {
        const string DefaultIconsLocation = "Assets/2DAssets/UI/_REWORK ROOT - put new assets here/KeyIcons";
        
        [SerializeField] List<string> inputFolders = new();
        [SerializeField] string outputTexturePath = DefaultIconsLocation;
        [SerializeField] string outputTextureName = "KeyIconsAtlas";
        [SerializeField] int iconSize = 64;
        [SerializeField] int margin = 2;

        [MenuItem("Tools/Create Texture Atlas")]
        public static void ShowWindow() {
            GetWindow<TextureAtlasCreator>("Texture Atlas Creator");
        }

        void OnGUI() {
            for (int i = 0; i < inputFolders.Count; i++) {
                EditorGUILayout.BeginHorizontal();
                
                string folderName = $"Folder {i + 1}:";
                EditorGUILayout.LabelField(folderName, inputFolders[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(60))) {
                    inputFolders.RemoveAt(i);
                    --i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Folder")) {
                string newFolder = EditorUtility.OpenFolderPanel("Select folder with icons", DefaultIconsLocation, "");
                if (!string.IsNullOrEmpty(newFolder) && !inputFolders.Contains(newFolder)) {
                    inputFolders.Add(newFolder.Replace(Application.dataPath, "Assets"));
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output atlas path:", outputTexturePath);
            if (GUILayout.Button("Choose", GUILayout.Width(60))) {
                string chosenPath = EditorUtility.OpenFolderPanel("Select output folder", DefaultIconsLocation, "");
                if (!string.IsNullOrWhiteSpace(chosenPath)) {
                    int index = chosenPath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
                    outputTexturePath = chosenPath[index..];
                }
            }
            EditorGUILayout.EndHorizontal();
            
            outputTextureName = EditorGUILayout.TextField("Output atlas name:", outputTextureName);
            iconSize = EditorGUILayout.IntField("Icon size:", iconSize);
            margin = EditorGUILayout.IntField("Margin between icons:", margin);

            if (GUILayout.Button("Generate Atlas")) {
                GenerateTextureAtlas(inputFolders.ToArray(), outputTexturePath, outputTextureName, iconSize, margin);
            }
        }

        static void GenerateTextureAtlas(string[] inputFolders, string outputPath, string outputName, int iconSize, int margin) {
            string filePath = $"{outputPath}/{outputName}.png";
            if (File.Exists(filePath)) {
                File.Delete(filePath);
                File.Delete($"{filePath}.meta");
                AssetDatabase.Refresh();
            }
            
            List<string> spriteGuidsList = new();

            foreach (string folder in inputFolders) {
                string[] spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] {folder});
                spriteGuidsList.AddRange(spriteGuids);
            }
            
            if (spriteGuidsList.Count == 0) {
                Log.Minor?.Error("No textures found in the specified folders.");
                return;
            }
            
            int totalIconsCount = spriteGuidsList.Count;
            var size = CalculateOptimalAtlasSize(totalIconsCount, iconSize, margin);
            int atlasWidth = size.x;

            Texture2D atlasTexture = new(atlasWidth, atlasWidth, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[atlasWidth * atlasWidth];
            Array.Fill(clearPixels, Color.clear);
            atlasTexture.SetPixels(clearPixels);
            
            SpriteMetaData[] atlasMetaData = new SpriteMetaData[totalIconsCount];
            int iconWithMargin = iconSize + margin;
            int columnsCount = atlasWidth / iconWithMargin;

            for (int i = 0; i < spriteGuidsList.Count; i++) {
                string guid = spriteGuidsList[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                string spriteName = Path.GetFileNameWithoutExtension(assetPath);
                Texture2D texture = ResizeTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath), iconSize, iconSize);
                SetTextureReadable(assetPath, true);
                
                int x = i % columnsCount * iconWithMargin;
                int y = i / columnsCount * iconWithMargin;
                
                Color[] iconPixels = texture.GetPixels();
                atlasTexture.SetPixels(x, y, iconSize, iconSize, iconPixels);
                
                atlasMetaData[i] = new SpriteMetaData {
                    name = spriteName,
                    rect = new Rect(x, y, iconSize, iconSize),
                    alignment = (int) SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
                
                SetTextureReadable(assetPath, false);
            }

            atlasTexture.Apply();
            atlasTexture = EditorAssetUtil.Create(atlasTexture, outputPath, outputName);

            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null) {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritesheet = atlasMetaData;
                importer.maxTextureSize = math.ceilpow2(atlasWidth);
                importer.crunchedCompression = true;
                importer.compressionQuality = 100;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Log.Minor?.Info($"Atlas created: {filePath} ({totalIconsCount} icons, {atlasWidth}x{atlasWidth})", atlasTexture);
        }

        static Vector2Int CalculateOptimalAtlasSize(int totalIcons, int iconSize, int margin) {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(totalIcons));

            int iconWithMargin = iconSize + margin;
            int width = columns * iconWithMargin;
            int height = columns * iconWithMargin;

            return new Vector2Int(RoundUpToMultipleOf4(width), RoundUpToMultipleOf4(height));
        }
        
        static int RoundUpToMultipleOf4(int value) {
            return (value + 3) & ~3;
        }

        static void SetTextureReadable(string path, bool isReadable) {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null) {
                importer.isReadable = isReadable;
            }
        }

        static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight) {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            UnityEngine.Graphics.Blit(source, rt);

            Texture2D resized = new(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            resized.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return resized;
        }
    }
}