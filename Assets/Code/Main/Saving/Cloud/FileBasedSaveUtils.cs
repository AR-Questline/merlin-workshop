using System.IO;
using System.Linq;
using Awaken.TG.Main.Saving.Cloud.Services;

namespace Awaken.TG.Main.Saving.Cloud {
    public static class FileBasedSaveUtils {
        public static int GetFileCount(string relativePath) {
            var folderPath = Path.Combine(CloudService.Get.DataPath, relativePath);
            if (Directory.Exists(folderPath)) {
                return Directory.GetFiles(folderPath).Count(s => !s.Contains("_uncompressed"));
            }

            return 0;
        }
    }
}