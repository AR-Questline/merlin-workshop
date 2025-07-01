using System.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Saving.LargeFiles {
    public partial class LargeFilesStorage {
        public void SaveFile(string folder, Texture2D texture, out int fileIndex) {
            var data = texture.EncodeToJPG();
            SaveFile(folder, data, LargeFileType.Texture2D, out fileIndex);
        }

        public bool TryLoadFile(int fileIndex, out Texture2D texture) {
            if (TryLoadFile(fileIndex, out var fileData, out byte[] data, LargeFileType.Texture2D) == false) {
                texture = null;
                return false;
            }

            texture = new Texture2D(2, 2);
            texture.LoadImage(data, true);
            texture.name = fileData.fileName;
            return texture;
        }

        public int GetFileCountInFolder(string folder) {
            return _filesDatas.Count(data => data.folder == folder);
        }

        public void ForceRemoveFile(int fileIndex) {
            if (fileIndex == 0) {
                return;
            }
            RemoveFile(fileIndex);
        }
    }
}



