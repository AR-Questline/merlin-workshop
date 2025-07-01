using System;
using System.Collections.Generic;
using System.IO;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Saving.Utils;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.MVC.Domains {
    public static class DomainUtils {
        // === Domains loading
        public static void LoadAppDomains() {
#if UNITY_EDITOR
            bool loadOnPlay = UnityEditor.EditorPrefs.GetBool("load.on.play", false);
            if (!loadOnPlay) {
                return;
            }
#endif

            try {
                StructList<(string expectedFolder, SaveSlot saveSlot)> loadedSaveSlots = new(100);
                
                foreach (var slot in CloudService.Get.CollectFoundSaveSlots()) {
                    try {
                        Domain domain = Domain.SaveSlotMetaData(slot);
                        // Load SaveSlotMetaData
                        // Do not cache, because it could be a false domain and we don't want to propagate it, if it's valid, it's gonna get serialized again later
                        LoadSave.Get.LoadSaveSlotMetadata(domain);
                        LoadSave.Get.ClearCache(domain);

                        loadedSaveSlots.Add((slot, LatestSaveSlot()));
                    } catch (Exception e) {
                        Log.Critical?.Error($"Error while loading metadata domain {Domain.SaveSlotMetaData(slot)}. Exception below");
                        Debug.LogException(e);
                    }
                }

#if !UNITY_GAMECORE && !UNITY_PS5
                // If we detect a need for folder change we move them to temporary folders as to not overwrite any existing save slots
                if (!Configuration.GetBool(ApplicationScene.IsGoG)) {
                    MoveMismatchedFoldersToTemp(loadedSaveSlots);
                    MoveTempFoldersToFinalDestination(loadedSaveSlots);
                }
#endif
                
                var lfs = World.Services.Get<LargeFilesStorage>();
                foreach (var slot in World.All<SaveSlot>()) {
                    if (slot.usedLargeFilesIndices.IsCreated) {
                        lfs.SetSaveSlotUsedFilesData(slot.SlotIndex, slot.usedLargeFilesIndices);
                    }
                }
                lfs.RemoveUnusedFiles();
            } catch (Exception e) {
                Debug.LogException(e);
                Log.Important?.Error("Corrupted save file (exception message above)");
            }
        }
        
        static void MoveMismatchedFoldersToTemp(StructList<(string expectedFolder, SaveSlot saveSlot)> loadedSaveSlots) {
            for (int i = 0; i < loadedSaveSlots.Count; i++) {
                var pair = loadedSaveSlots[i];
                string correctID = pair.saveSlot.ID;

                if (correctID != pair.expectedFolder) {
                    // If previuous migration failed for whatever reason
                    if (pair.expectedFolder.EndsWith("_temp")) {
                        continue;
                    }
                        
                    string oldSlotPath = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory, pair.expectedFolder);
                    if (!Directory.Exists(oldSlotPath)) {
                        continue; // Was already moved by a different system
                    }
                    string tempFolder = correctID + "_temp";
                    string newSlotPath = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory, tempFolder);

                    try {
                        Log.Important?.Warning($"Moving save slot {pair.saveSlot.ID} from {pair.expectedFolder} to {tempFolder} to fix folder name mismatch");
                            
                        if (Directory.Exists(newSlotPath)) {
                            throw new Exception($"Directory {newSlotPath} already exists");
                        }
                            
                        IOUtil.DirectoryCopy(oldSlotPath, newSlotPath);
                        pair.saveSlot.DiscardAfterFolderRename(oldSlotPath);
                        
                        loadedSaveSlots[i] = (tempFolder, pair.saveSlot);
                    } catch (Exception e) {
                        Log.Critical?.Error($"Error while moving save slot {pair.saveSlot.ID} from {pair.expectedFolder} to {tempFolder}. Exception below");
                        Debug.LogException(e);
                    }
                }
            }
        }

        static void MoveTempFoldersToFinalDestination(StructList<(string expectedFolder, SaveSlot saveSlot)> loadedSaveSlots) {
            // Move files to final destination and load again
            for (int i = 0; i < loadedSaveSlots.Count; i++) {
                var pair = loadedSaveSlots[i];
                if (!pair.expectedFolder.EndsWith("_temp")) {
                    continue;
                }
                string oldSlotPath = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory, pair.expectedFolder);
                string correctID = pair.saveSlot.ID;
                string newSlotPath = Path.Combine(CloudService.Get.DataPath, CloudService.SavedGamesDirectory, correctID);
                try {
                    Log.Important?.Warning($"Moving save slot {pair.saveSlot.ID} from {pair.expectedFolder} to {correctID} after folder name mismatch");
                        
                    if (Directory.Exists(newSlotPath)) {
                        throw new Exception($"Directory {newSlotPath} already exists");
                    }
                        
                    IOUtil.DirectoryCopy(oldSlotPath, newSlotPath);
                    Directory.Delete(oldSlotPath, true);
                        
                    Domain saveSlotMetaData = Domain.SaveSlotMetaData(pair.saveSlot.ID);
                    LoadSave.Get.LoadSaveSlotMetadata(saveSlotMetaData);
                    LoadSave.Get.ClearCache(saveSlotMetaData);
                } catch (Exception e) {
                    Log.Critical?.Error($"Error while moving save slot {pair.saveSlot.ID} from {pair.expectedFolder} to {correctID}. Exception below");
                    Debug.LogException(e);
                }
            }
        }

        static SaveSlot LatestSaveSlot() {
            var allInOrder = World.AllInOrder();
            for (int i = allInOrder.Count - 1; i >= 0; i--) {
                if (allInOrder[i] is SaveSlot slotModel) {
                    return slotModel;
                }
            }
            return null;
        }

        // === Domain Queries
        public static IEnumerable<Domain> GetSaveSlotChildren() {
            yield return Domain.Gameplay;
            
            // Scene domains
            foreach (var domain in SceneService.AllSceneDomains()) {
                yield return domain;
            }
        }

        public static IEnumerable<Domain> SaveSlotDomainsInUse() {
            yield return Domain.Gameplay;

            var sceneService = World.Services.TryGet<SceneService>();
            if (sceneService?.MainSceneRef != null) {
                yield return sceneService.MainDomain;
                if (sceneService.AdditiveSceneRef != null) {
                    yield return Domain.Scene(sceneService.AdditiveSceneRef);
                }
            }
        }
    }
}