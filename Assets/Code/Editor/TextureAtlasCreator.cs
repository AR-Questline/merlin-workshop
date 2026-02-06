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
        [SerializeField] List<Texture2D> additionalSprites = new();
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Additional Sprites", EditorStyles.boldLabel);
            
            for (int i = 0; i < additionalSprites.Count; i++) {
                EditorGUILayout.BeginHorizontal();
                additionalSprites[i] = (Texture2D)EditorGUILayout.ObjectField($"Sprite {i + 1}:", additionalSprites[i], typeof(Texture2D), false);
                if (GUILayout.Button("Remove", GUILayout.Width(60))) {
                    additionalSprites.RemoveAt(i);
                    --i;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Additional Sprite")) {
                additionalSprites.Add(null);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            outputTexturePath = EditorGUILayout.TextField("Output atlas path:", outputTexturePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60))) {
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
                GenerateTextureAtlas(inputFolders.ToArray(), additionalSprites.ToArray(), outputTexturePath, outputTextureName, iconSize, margin);
            }
        }

        static void GenerateTextureAtlas(string[] inputFolders, Texture2D[] additionalSprites, string outputPath, string outputName, int iconSize, int margin) {
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
            
            // Add additional sprites
            List<string> additionalSpriteGuids = new();
            foreach (Texture2D sprite in additionalSprites) {
                if (sprite != null) {
                    string spritePath = AssetDatabase.GetAssetPath(sprite);
                    string spriteGuid = AssetDatabase.AssetPathToGUID(spritePath);
                    if (!string.IsNullOrEmpty(spriteGuid) && !spriteGuidsList.Contains(spriteGuid)) {
                        additionalSpriteGuids.Add(spriteGuid);
                    }
                }
            }
            spriteGuidsList.AddRange(additionalSpriteGuids);
            
            if (spriteGuidsList.Count == 0) {
                Log.Minor?.Error("No textures found in the specified folders or additional sprites.");
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
                Texture2D originalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                SetTextureReadable(assetPath, true);
                
                var (resizedTexture, actualWidth, actualHeight) = ResizeTexturePreserveAspect(originalTexture, iconSize, iconSize);
                
                int x = i % columnsCount * iconWithMargin;
                int y = i / columnsCount * iconWithMargin;
                
                // Center the resized texture within the icon slot
                int offsetX = (iconSize - actualWidth) / 2;
                int offsetY = (iconSize - actualHeight) / 2;
                
                Color[] iconPixels = resizedTexture.GetPixels();
                atlasTexture.SetPixels(x + offsetX, y + offsetY, actualWidth, actualHeight, iconPixels);
                
                atlasMetaData[i] = new SpriteMetaData {
                    name = spriteName,
                    rect = new Rect(x + offsetX, y + offsetY, actualWidth, actualHeight),
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

        static (Texture2D texture, int actualWidth, int actualHeight) ResizeTexturePreserveAspect(Texture2D source, int maxWidth, int maxHeight) {
            float sourceAspect = (float)source.width / source.height;
            float targetAspect = (float)maxWidth / maxHeight;
            
            int actualWidth, actualHeight;
            
            if (sourceAspect > targetAspect) {
                // Source is wider - fit to width
                actualWidth = maxWidth;
                actualHeight = Mathf.RoundToInt(maxWidth / sourceAspect);
            } else {
                // Source is taller - fit to height
                actualHeight = maxHeight;
                actualWidth = Mathf.RoundToInt(maxHeight * sourceAspect);
            }
            
            RenderTexture rt = RenderTexture.GetTemporary(actualWidth, actualHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            UnityEngine.Graphics.Blit(source, rt);

            Texture2D resized = new(actualWidth, actualHeight, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, actualWidth, actualHeight), 0, 0);
            resized.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return (resized, actualWidth, actualHeight);
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