using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Utils;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Saving {
    public class DecryptSaveFile : OdinEditorWindow {
        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = ".data", RequireExistingPath = true), ShowIf(nameof(FolderPathEmpty))]
        public string filePath;
        [FolderPath(AbsolutePath = true, RequireExistingPath = true), ShowIf(nameof(FilePathEmpty))]
        public string folderPath;
        
        [MenuItem("TG/Saves/Decrypt Save File")]
        public static void Open() {
            OdinEditorWindow.GetWindow<DecryptSaveFile>();
        }

        [Button]
        public void Decrypt() {
            if (!FilePathEmpty) {
                DecryptFile(filePath);
            } else {
                var files = Directory.GetFiles(folderPath, "*.data", SearchOption.AllDirectories);
                foreach (var file in files) {
                    DecryptFile(file);
                }
            }

            OpenLocation();
        }
        
        [Button]
        void OpenLocation() {
            var pathToOpen = !FilePathEmpty ? $"{Path.GetDirectoryName(filePath)}" : folderPath;
            Process.Start(pathToOpen);
        }

        static void DecryptFile(string path) {
            try {
                string fileName = Path.GetFileNameWithoutExtension(path);
                string directory = Path.GetDirectoryName(path);
#if UNITY_EDITOR
                if (LoadSystem.EDITOR_TryLoadCompressedSaveDataFromFile(directory, fileName, out var compressedData)) {
                    using var decompressingStream = LoadSave.DecompressingSaveStream(compressedData);
                    string saveFileName = fileName + "_uncompressed";
                    IOUtil.Save(directory, saveFileName, decompressingStream);
                }
#endif
            } catch (Exception e) {
                Log.Important?.Error($"Cannot decrypt file: {path}");
                UnityEngine.Debug.LogException(e);
            }
        }

        bool FilePathEmpty => string.IsNullOrWhiteSpace(filePath);
        bool FolderPathEmpty => string.IsNullOrWhiteSpace(folderPath);
    }
}