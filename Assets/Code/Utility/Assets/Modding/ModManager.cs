using System.IO;
using System.Linq;
using Awaken.TG.Assets.Modding;
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
        public ModHandle[] OrderedMods { get; }
        
        public ModManager() {
            Addressables.InitializeAsync().WaitForCompletion();
            AllMods = ModDirectory.GetDirectories()
                .Select(dir => (dir.Name, dir.GetFiles().FirstOrDefault(file => file.Extension == ".json")))
                .Where(pair => pair.Item2 != null)
                .Select(pair => new Mod(pair.Item1, pair.Item2))
                .ToArray();
            Log.Important?.Warning($"Loaded Mods [{AllMods}]:\n\t" + string.Join("\n\t", AllMods.Select(mod => mod.Name)));
            OrderedMods = new ModHandle[AllMods.Length];
        }
        
        public void Refresh() {
            Addressables.RemoveModLocators();
            foreach (var ptr in OrderedMods) {
                if (ptr.active) {
                    ref readonly var mod = ref Mod(ptr);
                    Addressables.AddModLocator(mod.Locator);
                }
            }
        }

        public ref readonly Mod Mod(ModHandle ptr) {
            return ref AllMods[ptr.index];
        }

        public ModMetadata Metadata(ModHandle ptr) {
            return ModMetadata.Load(Mod(ptr).Name);
        }
    }
}