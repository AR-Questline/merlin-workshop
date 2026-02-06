using System.IO;
using Awaken.Utility.Archives;
using UnityEngine;
using Awaken.Utility.Debugging;

namespace Awaken.ECS.MedusaRenderer {
    public class MedusaPersistence {
        public const string SubdirectoryName = "Medusa";
        public const string ArchiveFileName = "medusa.arch";

        public static readonly string BakingDirectoryPath = Path.Combine("Library", SubdirectoryName);

        static MedusaPersistence s_instance;

        string _basePath;

        public static MedusaPersistence Instance {
            get {
                return s_instance ??= new MedusaPersistence();
            }
        }

        MedusaPersistence() {
            _basePath = BakingDirectoryPath;
            var success = ArchiveUtils.TryMountAndAdjustPath("Medusa", SubdirectoryName, ArchiveFileName, ref _basePath);
            if (!success) {
                Log.Critical?.Error($"Drake merged archive not found at {Path.Combine(Application.streamingAssetsPath, SubdirectoryName, ArchiveFileName)}");
            }
        }

        public string BaseScenePath(string sceneName) {
            return Path.Combine(_basePath, sceneName);
        }

        public static string TransformsPath(string basePath) {
            return Path.Combine(basePath, "transformsBuffer.medusa");
        }

        public static string MatricesPath(string basePath) {
            return Path.Combine(basePath, "matrices.medusa");
        }

        public static string RenderersPath(string basePath) {
            return Path.Combine(basePath, "renderers.medusa");
        }

        public static string ReciprocalUvDistributions(string basePath) {
            return Path.Combine(basePath, "reciprocalUvDistributions.medusa");
        }
    }
}
