namespace Awaken.TG.Main.Saving.LargeFiles {
    public struct LargeFileIndex {
        public int value;

        public LargeFileIndex(int value) {
            this.value = value;
        }

        public static implicit operator int(LargeFileIndex largeFileIndex) => largeFileIndex.value;
        
        public static implicit operator LargeFileIndex(int intValue) => new(intValue);
    }
}