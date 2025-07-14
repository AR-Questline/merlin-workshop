using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.MVC.Domains;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Saving.Cloud.Services {
    public abstract class CloudService {
        public const string SavedGamesDirectory = "Saved";
        public const string UnsynchronizedSavedGamesDirectory = "Unsynchronized";
        public string DataPath => Path.Combine(ApplicationDataPath, UserID);
        protected abstract string UserID { get; }
        protected abstract bool WorksOnFileSystem { get; } 
        string ApplicationDataPath { get; } = Application.persistentDataPath;

        // === Singleton Construction

        static CloudService s_instance;
        public static CloudService Get => s_instance 
            ?? EditorUtilityGet()
            ?? throw new System.Exception("CloudService not initialized");

        public static bool IsInitialized => s_instance != null;
        /// <summary>
        /// Should only be called by Application Scene
        /// </summary>
        public static void Initialize() {
            if (s_instance != null) throw new System.Exception("CloudService already initialized");
            s_instance = CreateCloudService();
        }

        public static void EDITOR_RuntimeReset() {
            s_instance = null;
        }

        static CloudService EditorUtilityGet() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return new DebugCloudService();
            }
#endif
            return null;
        }

        static CloudService CreateCloudService() {
            return new DebugCloudService();
        }
        
        public virtual async UniTask WaitForManagerInitialization() {
            await UniTask.CompletedTask;
        }

        public virtual IEnumerable<ICloudSyncResult> InitCloud() {
            return null;
        }

        /// <summary>
        /// Save a single file without opening the save slot for batch operations (prefs, LFS, save meta).
        /// </summary>
        public abstract void SaveGlobalFile(string relativePath, string fileName, byte[] data, bool synchronized = true);
        
        /// <summary>
        /// Save a file to the save slot in batch mode.
        /// </summary>
        public virtual void SaveSlotFile(string relativePath, string fileName, byte[] data) => SaveGlobalFile(relativePath, fileName, data);
        
        /// <summary>
        /// Try loading a single file, without opening the save slot for batch operations (prefs, LFS, save meta).
        /// </summary>
        public abstract bool TryLoadSingleFile(string relativePath, string fileName, out byte[] data, bool synchronized = true);
        
        /// <summary>
        /// Try loading a file that belongs to a save slot in batch mode.
        /// </summary>
        public virtual bool TryLoadSaveSlotFile(string relativePath, string fileName, out byte[] data) => TryLoadSingleFile(relativePath, fileName, out data);
        
        /// <summary>
        /// Delete a file that doesn't belong to a save slot (prefs, LFS). 
        /// </summary>
        public abstract void DeleteGlobalFile(string relativePath, string fileName, bool synchronized = true);
        
        /// <summary>
        /// Removes the save slot completely from both local and cloud.
        /// </summary>
        public abstract void DeleteSaveSlot(string relativePath, bool inBatch = false);
        
        /// <summary>
        /// It will be called asynchronously and finished before files get saved to the slot. Use this to clean up the slot.
        /// </summary>
        public virtual void PrepareSaveSlotForSave(string relativePath) { }

        /// <summary>
        /// Start batch mode for all file-write operations.
        /// </summary>
        public virtual void BeginSaveBatch() {}
        
        /// <summary>
        /// End batch mode for all file-write operations.
        /// </summary>
        public virtual void EndSaveBatch() {}
        
        /// <summary>
        /// Start batch mode for saving given save slot.
        /// </summary>
        public virtual void BeginSaveDirectory(string directory, long size) { }

        /// <summary>
        ///  End batch mode for saving given save slot.
        /// </summary>
        public virtual UniTask<EndSaveDirectoryResult> EndSaveDirectory(string directory, bool failed) {
            return UniTask.FromResult(new EndSaveDirectoryResult {
                Success = true,
                SaveResult = SaveResult.Default,
            });
        }

        /// <summary>
        /// Start batch mode for save slot loading.
        /// </summary>
        public virtual SaveResult BeginLoadDirectory(string directory) {
            return SaveResult.Default;
        }
        
        /// <summary>
        /// Ends batch mode for save slot loading.
        /// </summary>
        public virtual void EndLoadDirectory(string directory) { }
        
        public virtual IEnumerable<string> CollectFoundSaveSlots() {
            if (!WorksOnFileSystem) yield break;
            
            string domainPath = Domain.Main.ConstructSavePath(null);
            var savePath = Path.Combine(DataPath, domainPath);
            if (Directory.Exists(savePath)) {
                string saveSlotsPath = savePath;

                if (Directory.Exists(saveSlotsPath)) {
                    var directories = Directory.GetDirectories(saveSlotsPath);
                    foreach (var dir in directories) {
                        string dirName = new DirectoryInfo(dir).Name;
                        string domainName = Domain.SaveSlotMetaData(dirName).SaveName;
                        if (File.Exists(Path.Combine(dir, $"{domainName}.data"))) {
                            yield return dirName;
                        }
                    }
                }
            }
        }
    }
}
