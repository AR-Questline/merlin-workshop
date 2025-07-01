using System.IO;
using Awaken.Utility.Debugging;
using Unity.Content;
using Unity.IO.Archive;
using UnityEngine;

namespace Awaken.Utility.Archives {
    public static class ArchiveUtils {
        public static bool TryMountAndAdjustPath(string contentName, string subdirectoryName, string archiveFileName, ref string basePath) {
#if ARCHIVES_PRODUCED ||!UNITY_EDITOR
            ContentNamespace contentNamespace = ContentNamespace.GetOrCreateNamespace(contentName);
            var archivePath = Path.Combine(Application.streamingAssetsPath, subdirectoryName, archiveFileName);
            if (!File.Exists(archivePath)) {
                return false;
            }

            ArchiveHandle contentHandle = ArchiveFileInterface.MountAsync(contentNamespace, archivePath, string.Empty);
            contentHandle.JobHandle.Complete();
            if (contentHandle.Status != ArchiveStatus.Complete) {
                Log.Critical?.Error($"Archive mount at path [{archivePath}] failed with status {contentHandle.Status}");
                contentHandle.Unmount().Complete();
                contentNamespace.Delete();
                return false;
            }

            basePath = contentHandle.GetMountPath();
            return true;
#else
            return true;
#endif
        }
    }
}
