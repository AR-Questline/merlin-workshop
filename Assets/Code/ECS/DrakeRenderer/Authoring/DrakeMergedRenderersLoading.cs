using System.IO;
using Awaken.Utility;
using Awaken.Utility.Archives;
using UnityEngine;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeMergedRenderersLoading {
        public const string SubdirectoryName = "DrakeMR";
        public const string ArchiveFileName = "merged_drakes.arch";

        public static readonly string BakingDirectoryPath = Path.Combine("Library", SubdirectoryName);

        static DrakeMergedRenderersLoading s_instance;

        string _basePath;

        public static DrakeMergedRenderersLoading Instance {
            get {
                return s_instance ??= new DrakeMergedRenderersLoading();
            }
        }

        DrakeMergedRenderersLoading() {
            _basePath = BakingDirectoryPath;
            var success = ArchiveUtils.TryMountAndAdjustPath("DrakeMR", SubdirectoryName, ArchiveFileName, ref _basePath);
            if (!success) {
                Log.Critical?.Error($"Drake merged archive not found at {Path.Combine(Application.streamingAssetsPath, SubdirectoryName, ArchiveFileName)}");
            }
        }

        public string GetFilePath(in SerializableGuid guid) {
            var path = Path.Combine(_basePath, $"{guid:N}.data");
            return path;
        }
    }
}
