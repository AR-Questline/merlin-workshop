using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Awaken.TG.Main.Saving.Cloud.Services {
    public class SaveResult {
        public static readonly SaveResult Default = new() {
            FileNames = Array.Empty<string>(),
        };

        public int FileCount => FileNames.Length;
        public string[] FileNames { get; init; }
    }
}