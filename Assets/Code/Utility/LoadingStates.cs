namespace Awaken.Utility {
    public static class LoadingStates {
        public static bool IsLoadingWorld { get; set; }
        public static uint LoadingLocations { get; set; }

        public static bool IsLoadingHlods { get; set; }
        public static bool PauseHlodUpdateByInterior { get; set; }
        public static byte PauseHlodUpdateCounter { get; set; }
    }
}