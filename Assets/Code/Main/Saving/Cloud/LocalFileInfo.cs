using System;
using System.IO;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.Utils;

namespace Awaken.TG.Main.Saving.Cloud {
    public class LocalFileInfo {
        public string fullPath;
        public string directory;
        public string fileName;
        public string steamPath;

        public byte[] Data => IOUtil.Load(directory, fileName);
        public DateTime WindowsTimeStamp => Exists ? IOUtil.GetTimeStamp(directory, fileName) : default;
        public bool Exists => IOUtil.HasSave(directory, fileName);
            
        public LocalFileInfo(string fullPath) {
            this.fullPath = fullPath;
            directory = Path.GetDirectoryName(fullPath);
            fileName = Path.GetFileNameWithoutExtension(fullPath);
            steamPath = fullPath.Replace(CloudService.Get.DataPath, "")
                .Substring(1)
                .Replace("\\", "/")
                .Replace(".data", "");
        }

#if !UNITY_GAMECORE && !UNITY_PS5
        public LocalFileInfo(RemoteStorageFile cloudFile) {
            steamPath = cloudFile.name;
            fullPath = Path.Combine(CloudService.Get.DataPath, steamPath) + ".data";
            directory = Path.GetDirectoryName(fullPath);
            fileName = Path.GetFileNameWithoutExtension(fullPath);
        }
#endif
    }
}