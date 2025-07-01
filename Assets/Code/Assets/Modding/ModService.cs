using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Assets.Modding;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Awaken.TG.Assets.Modding {
    public partial class ModService : SerializedService {
        public override ushort TypeForSerialization => SavedServices.ModService;

        // needed for addressable loading
        [UnityEngine.Scripting.Preserve]
        public static string ModDirectoryPath => ModManager.ModDirectoryPath;
        
        // === State
        ModManager _manager;

        // === Constructor
        public ModService() {
            _manager = new ModManager();
            Load();
        }

        public void Load() {
            int orderedModIndex = 0;
            int count = PrefMemory.GetInt("mods", 0);
            for (int i = 0; i < count; i++) {
                var name = PrefMemory.GetString($"mod[{i}].name");
                var active = PrefMemory.GetBool($"mod[{i}].active");
                int modIndex = -1;
                for (int j = 0; j < _manager.AllMods.Length; j++) {
                    if (_manager.AllMods[j].Name == name) {
                        modIndex = j;
                        break;
                    }
                }
                if (modIndex != -1) {
                    _manager.OrderedMods[orderedModIndex++] = new ModStatus(modIndex, active);
                }
            }
            
            int loadedMods = orderedModIndex;
            if (loadedMods < _manager.AllMods.Length) {
                for (int i = 0; i < _manager.AllMods.Length; i++) {
                    bool shouldAdd = true;
                    for (int j = 0; j < loadedMods; j++) {
                        if (_manager.OrderedMods[j].index == i) {
                            shouldAdd = false;
                            break;
                        }
                    }
                    if (shouldAdd) {
                        _manager.OrderedMods[orderedModIndex++] = new ModStatus(i, true);
                    }
                }
            }

            _manager.Refresh();
        }
        
        public void Save() {
            PrefMemory.Set("mods", _manager.OrderedMods.Length, true);
            for (int i = 0; i < _manager.OrderedMods.Length; i++) {
                var status = _manager.OrderedMods[i];
                PrefMemory.Set($"mod[{i}].name", status.Data(_manager).Name, true);
                PrefMemory.Set($"mod[{i}].active", status.active, true);
            }
        }
        
        // === Utils
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