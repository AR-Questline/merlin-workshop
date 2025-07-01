using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.FBX {
    public class AnimationClipManager : EditorWindow {
        const int Width = 500;
        const int Height = 75;
        static AnimationClipManager s_editor;
        static List<string> s_allFiles = new List<string>();
        static string s_path = "3DAssets/Characters/Animations/Rokoko";
        static int s_x, s_y;

        [MenuItem("TG/Assets/AnimationClip Renamer")]
        static void ShowEditor() {
            s_editor = EditorWindow.GetWindow<AnimationClipManager>();
            CenterWindow();
        }

        void OnGUI() {
            s_path = EditorGUILayout.TextField("Animations Directory: ", s_path);
            if (GUILayout.Button("Rename")) {
                Rename();
            }
        }

        public static void Rename() {
            DirSearch();

            if (s_allFiles.Count > 0) {
                foreach (string t in s_allFiles) {
                    int idx = t.IndexOf("Assets", StringComparison.InvariantCulture);
                    string asset = t.Substring(idx);
                    var fileName = Path.GetFileNameWithoutExtension(t);
                    var importer = (ModelImporter) AssetImporter.GetAtPath(asset);
                    RenameAndImport(importer, fileName);
                }
            }
        }

        static void RenameAndImport(ModelImporter asset, string clipName) {
            ModelImporter modelImporter = asset;
            ModelImporterClipAnimation[] clipAnimations = modelImporter.defaultClipAnimations;

            for (int i = 0; i < clipAnimations.Length; i++) {
                clipAnimations[i].name = clipName;
            }

            modelImporter.clipAnimations = clipAnimations;
            modelImporter.SaveAndReimport();
        }

        static void CenterWindow() {
            s_editor = EditorWindow.GetWindow<AnimationClipManager>();
            s_x = (Screen.currentResolution.width - Width) / 2;
            s_y = (Screen.currentResolution.height - Height) / 2;
            s_editor.position = new Rect(s_x, s_y, Width, Height);
            s_editor.maxSize = new Vector2(Width * 2f, Height);
            s_editor.minSize = new Vector2(Width/2f, Height);
            s_editor.titleContent = new GUIContent("AnimationClip Renamer");
        }

        static void DirSearch() {
            string info = Application.dataPath;
            if (!string.IsNullOrWhiteSpace(s_path)) {
                info += $"/{s_path}";
            }
            string[] fileInfo = Directory.GetFiles(info, "*.fbx", SearchOption.AllDirectories);
            foreach (string file in fileInfo) {
                if (file.EndsWith(".fbx"))
                    s_allFiles.Add(file);
            }
        }
    }
}
