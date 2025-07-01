using System.IO;
using UnityEngine;

namespace Awaken.TG.LeshyRenderer {
    public static class LeshyPersistence {
        public const string CellsCatalogBinFile = "CellsCatalog.leshy";
        public const string MatricesBinFile = "Matrices.bin";

        public static string BasePath(string sceneName) {
            return Path.Combine(Application.streamingAssetsPath, "Leshy", sceneName);
        }
    }
}
