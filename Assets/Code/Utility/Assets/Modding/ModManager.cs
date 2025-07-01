using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Assets.Modding;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.Utility.Assets.Modding {
    public class ModManager {
        public static string ModDirectoryPath => Application.persistentDataPath + "/Mods";
        static DirectoryInfo ModDirectory {
            get {
                if (!Directory.Exists(ModDirectoryPath)) {
                    Directory.CreateDirectory(ModDirectoryPath);
                }
                return new DirectoryInfo(ModDirectoryPath);
            }
        }
        
        public Mod[] AllMods { get; }
        public ModStatus[] OrderedMods { get; }
        
        public ModManager() {
            Addressables.InitializeAsync().WaitForCompletion();
            AllMods = ModDirectory.GetDirectories()
                .Select(dir => (dir.Name, dir.GetFiles().FirstOrDefault(file => file.Extension == ".json")))
                .Where(pair => pair.Item2 != null)
                .Select(pair => new Mod(pair.Item1, pair.Item2))
                .ToArray();
            Log.Important?.Warning($"Loaded Mods [{AllMods}]:\n\t" + string.Join("\n\t", AllMods.Select(mod => mod.Name)));
            OrderedMods = new ModStatus[AllMods.Length];
        }
        
        public void Refresh() {
            Addressables.RemoveModLocators();
            foreach (var status in OrderedMods) {
                if (status.active) {
                    ref readonly var mod = ref status.Data(this);
                    Addressables.AddModLocator(mod.Locator);
                }
            }
        }
    }
}