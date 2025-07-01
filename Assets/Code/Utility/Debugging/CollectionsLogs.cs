using Unity.Burst;

namespace Awaken.Utility.Debugging {
    public static class CollectionsLogs {
        [BurstDiscard]
        public static void LogErrorIndexIsOutOfRangeForCapacity(int index, int capacity) {
            Log.Important?.Error($"Index {index} is out of range. Capacity = {capacity}");
        }
        
        [BurstDiscard]
        public static void LogErrorIndexIsOutOfRange(int index, int length) {
            Log.Important?.Error($"Index {index} is out of range [0, {length})");
        }

        [BurstDiscard]
        public static void LogErrorTrimmingSubArrayLength(int startIndex, int wantedLength, int arrayLength) {
            Log.Important?.Error($"Length {wantedLength} is too big for start index {startIndex} and array with length {arrayLength}. Trimming length");
        }
    }
}