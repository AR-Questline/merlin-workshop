namespace Awaken.TG.Main.Memories.FilePrefs {
    public static class DebugProjectNames {
        public const string DebugProjectNamesID = "debug.project.names";
        public static bool Basic { get; private set; }
        public static bool Extended { get; private set; }

        public static void SyncDebugNamesCache() {
            Basic = FileBasedPrefs.GetBool(DebugProjectNamesID, false);
        }

        public static void SetActiveBasic(bool value) {
            FileBasedPrefs.SetBool(DebugProjectNamesID, value, false);
            Basic = value;
        }

        public static void SetActiveExtended(bool value) {
            Extended = value;
        }
    }
}