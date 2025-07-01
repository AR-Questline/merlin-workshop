using System.IO;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    public static class GitDebugData {
        public static string InfoPath => Path.Join(Application.streamingAssetsPath, "vc.info");
        public static string BuildBranchName => ExtractBranch();
        public static string BuildCommitHash => ExtractCommitHash();

        public static void CopyBuildCommitHash() {
            GUIUtility.systemCopyBuffer = GitDebugData.BuildCommitHash;
        }

        static string ExtractBranch() {
            var path = InfoPath;
            if (File.Exists(path) == false) {
                return "Unknown";
            }

            var text = File.ReadAllText(path);
            var splitIndex = text.IndexOf("|", System.StringComparison.CurrentCulture);
            if (splitIndex == -1) {
                return "Unknown";
            }
            return text[..splitIndex];
        }

        static string ExtractCommitHash() {
            var path = InfoPath;
            if (File.Exists(path) == false) {
                return "-1";
            }

            var text = File.ReadAllText(path);
            var splitIndex = text.IndexOf("|", System.StringComparison.CurrentCulture);
            if (splitIndex == -1) {
                return "-1";
            }
            return text[(splitIndex+1)..];
        }
    }
}