using System;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher066_067 : Patcher {
        protected override Version MaxInputVersion => new (0, 66, 9999);
        protected override Version FinalVersion => new (0, 67);

        public override void StartGamePatch() {
            if (PlatformUtils.IsWindows) {
                string relative = Domain.Main.ConstructSavePath(null);
                string parentDir = Path.Combine(CloudService.Get.DataPath, relative);
                string profileDir = Path.Combine(parentDir, "Player_0");

                if (!Directory.Exists(profileDir)) {
                    return;
                }
                
                foreach (var file in Directory.GetFiles(profileDir, "*.*", SearchOption.AllDirectories)) {
                    if (file.EndsWith("Globals.data")) {
                        continue;
                    }
                    var dirParts = file.Split(Path.DirectorySeparatorChar).ToList();
                    int index = dirParts.IndexOf("Player_0");
                    dirParts.RemoveAt(index);
                    string newPath = Path.Combine(dirParts.ToArray());

                    string newPathDir = Path.GetDirectoryName(newPath);
                    if (!Directory.Exists(newPathDir)) {
                        Directory.CreateDirectory(newPathDir!);
                    }
                    
                    File.Move(file, newPath);
                }
                Directory.Delete(profileDir, true);
            }
            
#if UNITY_PS5
            Patcher_DeleteSaves.WipeSaves();
#endif
        }
    }
}