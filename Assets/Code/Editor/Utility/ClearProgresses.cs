using System.IO;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.MVC.Domains;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public static class ClearProgresses {
        const string ShowSlotsMenuName = "TG/Saves/Show Save Slot In Explorer";
        
        // === Menus
        [MenuItem("TG/Saves/Clear All Progress", false, 105)]
        static void ClearAll() {
            string absolutePath = Path.Combine(Application.persistentDataPath, "Editor");
            Directory.Delete(absolutePath, true);
            ClearPrefs();
        }

        [MenuItem("TG/Saves/Clear Prefs", false, 115)]
        public static void ClearPrefs() {
            PlayerPrefs.DeleteAll();
            FileBasedPrefs.DeleteAll(false);
            FileBasedPrefs.DeleteAll(true);
        }

        [MenuItem(ShowSlotsMenuName, false, 200)]
        static void ShowSaveSlotsLocation() {
            string path;
            if (Application.isPlaying) {
                path = Path.Combine(CloudService.Get.DataPath, Domain.SaveSlot.ConstructSavePath(SaveSlot.LastSaveSlotOfCurrentHero));
            } else {
                path = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory);
            }
            EditorUtility.RevealInFinder(path);
        }
    }
}