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
        void OpenLocation() {
            var pathToOpen = !FilePathEmpty ? $"{Path.GetDirectoryName(filePath)}" : folderPath;
            Process.Start(pathToOpen);
        }

        bool FilePathEmpty => string.IsNullOrWhiteSpace(filePath);
        bool FolderPathEmpty => string.IsNullOrWhiteSpace(folderPath);
    }
}