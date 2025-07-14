using System;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.Utility;
using Awaken.Utility.Assets.Modding;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Awaken.TG.Assets.Modding {
    public partial class ModService : SerializedService {
        public override ushort TypeForSerialization => SavedServices.ModService;
        
        [UnityEngine.Scripting.Preserve] // needed for addressable loading
        public static string ModDirectoryPath => ModManager.ModDirectoryPath;
        
        public ModManager Manager { get; }

        public ModService() {
            Manager = new ModManager();
            Load();
        }

        public void Load() {
            int orderedModIndex = 0;
            int count = PrefMemory.GetInt("mods", 0);
            for (int i = 0; i < count; i++) {
                var name = PrefMemory.GetString($"mod[{i}].name");
                var active = PrefMemory.GetBool($"mod[{i}].active");
                int modIndex = -1;
                for (int j = 0; j < Manager.AllMods.Length; j++) {
                    if (Manager.AllMods[j].Name == name) {
                        modIndex = j;
                        break;
                    }
                }
                if (modIndex != -1) {
                    Manager.OrderedMods[orderedModIndex++] = new ModHandle(modIndex, active);
                }
            }
            
            int loadedMods = orderedModIndex;
            if (loadedMods < Manager.AllMods.Length) {
                for (int i = 0; i < Manager.AllMods.Length; i++) {
                    bool shouldAdd = true;
                    for (int j = 0; j < loadedMods; j++) {
                        if (Manager.OrderedMods[j].index == i) {
                            shouldAdd = false;
                            break;
                        }
                    }
                    if (shouldAdd) {
                        Manager.OrderedMods[orderedModIndex++] = new ModHandle(i, true);
                    }
                }
            }

            Manager.Refresh();
        }
        
        public void Save() {
            PrefMemory.Set("mods", Manager.OrderedMods.Length, false);
            for (int i = 0; i < Manager.OrderedMods.Length; i++) {
                var ptr = Manager.OrderedMods[i];
                PrefMemory.Set($"mod[{i}].name", Manager.Mod(ptr).Name, false);
                PrefMemory.Set($"mod[{i}].active", ptr.active, false);
            }
        }
        
#if UNITY_EDITOR
        static bool s_addressablesInitialized;
#endif
        public static IResourceLocation GetAddressableLocation(string key, Type type = null) {
            if (key == null) return null;
            {
#if UNITY_EDITOR
                if (!s_addressablesInitialized) {
#endif
                    Addressables.InitializeAsync().WaitForCompletion();
#if UNITY_EDITOR
                    s_addressablesInitialized = true;
                }
#endif
                foreach (var locator in Addressables.ResourceLocators) {
                    if (locator.Locate(key, type, out var locations)) {
                        return locations[0];
                    }
                }
                return null;
            }
        }
    }
}