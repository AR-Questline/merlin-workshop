using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.TG.Main.Saving.LargeFiles {
    public partial struct LargeFilesIndices {
        public ushort TypeForSerialization => SavedTypes.LargeFilesIndices;

        [Saved] public UnsafeBitmask value;
        
        public LargeFilesIndices(UnsafeBitmask value) {
            this.value = value;
        }

        public static implicit operator UnsafeBitmask(LargeFilesIndices largeFilesIndices) => largeFilesIndices.value;
        
        public static implicit operator LargeFilesIndices(UnsafeBitmask mask) => new(mask);
    }
}