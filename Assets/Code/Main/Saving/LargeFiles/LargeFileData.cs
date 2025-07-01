using System;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.TG.Main.Saving.LargeFiles {
    [Serializable]
    public partial struct LargeFileData {
        public ushort TypeForSerialization => SavedTypes.LargeFileData;

        [Saved] public string folder;
        [Saved] public string fileName;
        [Saved] public LargeFileType type;
        public bool IsValid => string.IsNullOrEmpty(fileName) == false;
        
        public LargeFileData(string folder, string fileName, LargeFileType type) {
            this.folder = folder;
            this.fileName = fileName;
            this.type = type;
        }
    }
}