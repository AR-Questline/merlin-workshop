using System;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Saving.Cloud.Services;

namespace Awaken.TG.Main.Saving.Cloud {
    public static class FileBasedSaveUtils {
        public static string[] GetFiles(string relativePath) {
            string[] fileNames;
            var folderPath = Path.Combine(CloudService.Get.DataPath, relativePath);
            if (Directory.Exists(folderPath)) {
                fileNames = Directory.GetFiles(folderPath)
                    .Select(Path.GetFileName)
                    .Where(s => !s.Contains(LoadSystem.UncompressedFileSuffix))
                    .ToArray();
            } else {
                fileNames = Array.Empty<string>();
            }

            return fileNames;
        }
    }
}