namespace Awaken.TG.Main.Saving.Cloud.Services {
    public class SaveResult {
        public static readonly SaveResult Default = new() {
            SupportsFileCounting = false,
            FileCount = 0
        };

        public bool SupportsFileCounting { get; init; } = true; // TODO: Fix xbox and remove
        public int FileCount { get; init; }
    }
}