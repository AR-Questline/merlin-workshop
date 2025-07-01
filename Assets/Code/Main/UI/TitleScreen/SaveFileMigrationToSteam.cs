#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    public static class SaveFileMigrationToSteam {
        public class GoGSaveMigration {
            (string slotName, string path)[] _saveSlots;
            bool _foundFiles;
            
            public bool FilesExist() {
                // if (!GogGalaxyManager.IsInitialized()) {
                //     return false;
                // }
                // const string GOGRelativePath = @"\GOG.com\Galaxy\Applications\" + GogGalaxyManager.clientID + @"\Storage\Shared\Files\" + CloudService.SavedGamesDirectory;
                // const string GOGPathLocation = "%localappdata%";
                //
                // string gogPath = Environment.ExpandEnvironmentVariables(GOGPathLocation + GOGRelativePath);
                // var cloudService = new GogCloudService();
                //     
                // // We filter out any player changed hierarchy or legacy save slots
                // _saveSlots = cloudService.CollectFoundSaveSlots()
                //                          .WhereNotNull()
                //                          .Select(slot => (slot, path: Path.Combine(gogPath, slot)))
                //                          .Where(pair => !pair.slot.IsNullOrWhitespace() && Directory.Exists(pair.path))
                //                          .ToArray();
                //     
                // _foundFiles = _saveSlots.Length > 0;
                return _foundFiles;
            }
            
            public void Migrate() {
                if (!_foundFiles) {
                    return; // no saves to migrate
                }
                
                string destinationPath = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory);

                CloudService.Get.BeginSaveBatch();
                for (int i = 0; i < _saveSlots.Length; i++) {
                    string slotName = _saveSlots[i].slotName;
                    string saveSlotFolder = slotName + "_GoGMigration";
                    try {
                        // move all files
                        string sourcePath = _saveSlots[i].path;
                        string destinationSlotPath = Path.Combine(destinationPath, saveSlotFolder);
                        if (Directory.Exists(destinationSlotPath)) {
                            Log.Important?.Warning("Save slot already exists: " + saveSlotFolder);
                            continue; // skip if already exists
                        }
                        
                        Log.Important?.Warning("Migrating GoG save slot: " + slotName);
                        IOUtil.DirectoryCopy(sourcePath, destinationSlotPath);
                        
                        // delete files from gog directory
                        Directory.Delete(sourcePath, true);
                    } catch (Exception e) {
                        Log.Critical?.Error("Failed to migrate save slot: " + slotName);
                        Debug.LogException(e);
                    }
                }
                CloudService.Get.EndSaveBatch();
            }
        }

        public class DefaultSaveMigration {
            const string DefaultPath = "Data/" + CloudService.SavedGamesDirectory;
            bool _anyFilesExist;
            public bool FilesExist() {
                string fullPath = Path.Combine(Application.persistentDataPath, DefaultPath);
                _anyFilesExist = Directory.Exists(fullPath) && Directory.GetFiles(fullPath, "*MetaData.data", SearchOption.AllDirectories).Length > 0;
                return _anyFilesExist;
            }

            public void Migrate() {
                if (!_anyFilesExist) {
                    return; // no saves to migrate
                }
                string sourcePath = Path.Combine(Application.persistentDataPath, DefaultPath);
                string destinationPath = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory);

                // Migrate saves to Steam directory
                CloudService.Get.BeginSaveBatch();
                var slots = Directory.EnumerateDirectories(sourcePath).ToArray();
                
                for (int i = 0; i < slots.Length; i++) {
                    try {
                        string folderName = Path.GetFileName(slots[i]);
                        string saveSlotFolder = folderName + "_DefaultMigration";
                        // move all files
                        string destinationSlotPath = Path.Combine(destinationPath, saveSlotFolder);
                        if (Directory.Exists(destinationSlotPath)) {
                            Log.Important?.Warning("Save slot already exists: " + saveSlotFolder);
                            continue; // skip if already exists
                        }

                        Log.Important?.Warning("Migrating Default save slot: " + folderName);
                        IOUtil.DirectoryCopy(slots[i], destinationSlotPath);

                        // delete files from default directory

                        Directory.Delete(slots[i], true);
                    } catch (Exception e) {
                        Log.Critical?.Error("Failed to migrate save slot: " + slots[i]);
                        Debug.LogException(e);
                    }
                }
                CloudService.Get.EndSaveBatch();
            }
        }
    }
}
#endif