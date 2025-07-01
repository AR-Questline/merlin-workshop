using System;
using System.IO;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Utility.Patchers {
    // Delete Saves patcher template
    public abstract class Patcher_DeleteSaves : Patcher {
        public override void StartGamePatch() {
            WipeSaves();
        }

        public static void WipeSaves() {
            Log.Marking?.Warning("Wipe Saves");
            FileBasedPrefs.DeleteAll(true);
            FileBasedPrefs.DeleteAll(false);
            LoadSave.Get.DeleteAllSaveSlots();
            try {
                if (Directory.Exists(CloudService.Get.DataPath)) {
                    Directory.Delete(CloudService.Get.DataPath, true);
                }
            } catch (Exception) { /*ignore*/ }
        }
    }
}