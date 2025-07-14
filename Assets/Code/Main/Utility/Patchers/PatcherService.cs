using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public class PatcherService : IService {
        
        // === State
        string OriginalVersion { get; set; }

        Patcher[] _patchers = {
            // new Patcher019_020(),
            // new Patcher021_022(),
            // new Patcher022_023(),
            // new Patcher024_025(),
            // new Patcher026_027(),
            // new Patcher028_029(),
            // new Patcher036_037(),
            // new Patcher040_041(),
            // new Patcher041_042(),
            new Patcher051_052(),
            new Patcher054_055(),
            new Patcher066_067(),
            new Patcher100_101(),
            new Patcher101_102(),
            new Patcher102_103(),
            new Patcher104_105(),
            new Patcher106_106(),
            new Patcher_Final(), // put final as last
        };

        public static string CurrentVersionStr => Application.version;
        public static Version CurrentVersion => new Version(CurrentVersionStr);
        
        // === Constructor
        public PatcherService(GameConstants constants) {
            RunGamePatch(constants);
        }

        // === Patching methods
        void RunGamePatch(GameConstants constants) {
            OriginalVersion = PrefMemory.GetString("Version", CurrentVersionStr);
            var version = new Version(OriginalVersion);
            
            var lastWipeSavesVersion = new Version(constants.wipeSavesOnVersion);
            if (version < lastWipeSavesVersion || Configuration.GetBool("wipe_saves")) {
                Patcher_DeleteSaves.WipeSaves();
            }
            
            foreach (Patcher patcher in IteratePatchers(version)) {
                patcher.StartGamePatch();
                version = patcher.PatcherFinalVersion;
            }
            PrefMemory.Set("Version", version.ToString(), true);
        }


        public void BeforeDeserializedModel(Version version, Model model) {
            foreach (var patcher in IteratePatchers(version)) {
                patcher.BeforeDeserializedModel(model);
            }
        }
        
        public bool AfterDeserializedModel(Version version, Model model) {
            foreach (var patcher in IteratePatchers(version)) {
                if (patcher.AfterDeserializedModel(model) == false) {
                    Log.Important?.Error($"{patcher.GetType().Name} removed model {model.ID} imported from version {version}");
                    return false;
                }
            }
            return true;
        }

        public void AfterRestorePatch(Version version) {
            foreach (var patcher in IteratePatchers(version)) {
                patcher.AfterRestorePatch();
            }
        }

        // === Helpers
        IEnumerable<Patcher> IteratePatchers(Version version) {
            foreach (Patcher patcher in _patchers) {
                if (patcher.CanPatch(version)) {
                    yield return patcher;
                    version = patcher.PatcherFinalVersion;
                }
            }
        }
    }
}
