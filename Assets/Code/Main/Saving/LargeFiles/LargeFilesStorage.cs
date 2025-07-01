using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Scenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Saving.LargeFiles {
    public partial class LargeFilesStorage : IDomainBoundService {
        public const string LargeFilePlayerPrefsKey = "largeFilesData";
        const string LargeFileNameBase = "lf_";

        public Domain Domain => Domain.Globals;

        List<LargeFileData> _filesDatas = new List<LargeFileData>();

        List<UnsafeSparseBitmask> _filesReferencingSlots = new List<UnsafeSparseBitmask>();
        OnDemandCache<Domain, UnsafeBitmask> _domainToUsedLargeFilesMap = new(_ => new UnsafeBitmask(1, ARAlloc.Persistent));

        Domain _currentlySerializedDomain;
        UnsafeBitmask _largeFilesDatasOccupiedStatuses;
        UnsafeBitmask _runtimeUsedLargeFilesIndices;

        IEventListener _onDomainChangedListener;

        public void Init() {
            _largeFilesDatasOccupiedStatuses = new UnsafeBitmask(1, ARAlloc.Persistent);
            // Reserving index 0 for not set file index
            _filesReferencingSlots.Add(default);
            _largeFilesDatasOccupiedStatuses.Up(0);
            _runtimeUsedLargeFilesIndices = new UnsafeBitmask(1, ARAlloc.Persistent);
            _onDomainChangedListener = World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterNewDomainSet, OnNewDomainSet);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            Initialize();
        }

        public void SetSaveSlotUsedFilesData(uint saveSlotIndex, in UnsafeBitmask saveSlotUsedLargeFiles) {
            foreach (int fileIndex in saveSlotUsedLargeFiles.EnumerateOnes()) {
                try {
                    if (fileIndex >= _filesReferencingSlots.Count) {
                        Log.Important?.Error($"Save slot {saveSlotIndex} has a reference to large file {fileIndex} which was deleted");
                        continue;
                    }

                    if (fileIndex == 0) {
                        continue;
                    }
                    var referencingSaveSlotsIndices = _filesReferencingSlots[fileIndex];
                    referencingSaveSlotsIndices[saveSlotIndex] = true;
                    _filesReferencingSlots[fileIndex] = referencingSaveSlotsIndices;
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public void RemoveSaveSlotUsedFilesData(uint saveSlotIndex, in UnsafeBitmask saveSlotUsedLargeFiles) {
            foreach (int fileIndex in saveSlotUsedLargeFiles.EnumerateOnes()) {
                try {
                    if (fileIndex >= _filesReferencingSlots.Count) {
                        Log.Important?.Error($"Save slot {saveSlotIndex} has a reference to large file {fileIndex} which was deleted");
                        continue;
                    }

                    if (fileIndex == 0) {
                        continue;
                    }
                    var referencingSaveSlotsIndices = _filesReferencingSlots[fileIndex];
                    referencingSaveSlotsIndices[saveSlotIndex] = false;
                    _filesReferencingSlots[fileIndex] = referencingSaveSlotsIndices;
                    RemoveFileIfNotUsed(in referencingSaveSlotsIndices, fileIndex);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        public void OnSaveSlotChange(UnsafeBitmask? saveSlotUsedLargeFilesIndices) {
            var prevRuntimeUsedLargeFilesIndices = _runtimeUsedLargeFilesIndices.DeepClone(ARAlloc.Temp);
            _runtimeUsedLargeFilesIndices.Clear();
            if (saveSlotUsedLargeFilesIndices.HasValue) {
                UnsafeBitmask.OrWithChangeSize(ref _runtimeUsedLargeFilesIndices, saveSlotUsedLargeFilesIndices.Value);
            }

            // A file is not removed if it has ref count 0 but is used in runtime.
            // If save slot changes, used in runtime files become the same as referenced in save slot,
            // therefore, some files have ref count 0 and are not referenced in runtime,
            // so it is needed to clean up these files.
            var changedFilesIndicesBitmask = UnsafeBitmask.XorAssumePadZeros(
                in prevRuntimeUsedLargeFilesIndices, in _runtimeUsedLargeFilesIndices, ARAlloc.Temp);

            foreach (int fileIndex in changedFilesIndicesBitmask.EnumerateOnes()) {
                if (fileIndex == 0) {
                    continue;
                }
                if (_runtimeUsedLargeFilesIndices[(uint)fileIndex] == false) {
                    if (_filesReferencingSlots[fileIndex].HasOnes() == false) {
                        RemoveFile(fileIndex);
                    }
                }
            }


            changedFilesIndicesBitmask.Dispose();
            prevRuntimeUsedLargeFilesIndices.Dispose();
        }

        public bool RemoveOnDomainChange() {
            _runtimeUsedLargeFilesIndices.Clear();
            RemoveUnusedFiles();
            World.EventSystem.TryDisposeListener(ref _onDomainChangedListener);
            _filesDatas = null;
            for (int i = 1; i < _filesReferencingSlots.Count; i++) {
                if (_largeFilesDatasOccupiedStatuses[(uint)i]) {
                    _filesReferencingSlots[i].Dispose();
                }
            }

            _largeFilesDatasOccupiedStatuses.Dispose();
            _runtimeUsedLargeFilesIndices.Dispose();
            return true;
        }

        public void InitializeBeforeSerializingDomain(Domain domain, out UnsafeBitmask backupData) {
            var usedLargeFilesIndices = _domainToUsedLargeFilesMap[domain];
            var prevUsedLargeFilesIndices = usedLargeFilesIndices.DeepClone(ARAlloc.Persistent);
            usedLargeFilesIndices.Clear();
            // In saveSystem.Serialize(), _domainToUsedLargeFilesMap is updated through
            // calls to AddUsedLargeFile and AddUsedLargeFiles
            _domainToUsedLargeFilesMap[domain] = usedLargeFilesIndices;
            _currentlySerializedDomain = domain;
            backupData = prevUsedLargeFilesIndices;
        }

        public void AfterDomainSerialized(Domain domain, bool serializedSuccessfully, ref UnsafeBitmask backupData) {
            _currentlySerializedDomain = default;
            if (serializedSuccessfully) {
                backupData.Dispose();
            } else {
                // If domain had nothing to serialize, reset to previously serialized largeFilesIndices
                // because _serializedDomains is not changing in this case, so we are doing the same with
                // large files indices. TODO maybe not necessary?
                _domainToUsedLargeFilesMap[domain].Dispose();
                _domainToUsedLargeFilesMap[domain] = backupData;
            }
        }

        public void AddUsedLargeFile(int largeFileIndex) {
            var usedLargeFiles = _domainToUsedLargeFilesMap[_currentlySerializedDomain];
            usedLargeFiles.EnsureCapacity((uint)largeFileIndex + 1);
            usedLargeFiles.Up((uint)largeFileIndex);
            _domainToUsedLargeFilesMap[_currentlySerializedDomain] = usedLargeFiles;
        }

        public void AddUsedLargeFiles(in UnsafeBitmask largeFilesIndices) {
            var usedLargeFiles = _domainToUsedLargeFilesMap[_currentlySerializedDomain];
            UnsafeBitmask.OrWithChangeSize(ref usedLargeFiles, in largeFilesIndices);
            _domainToUsedLargeFilesMap[_currentlySerializedDomain] = usedLargeFiles;
        }

        public UnsafeBitmask GetUsedLargeFilesForDomain(Domain domain) {
            return _domainToUsedLargeFilesMap[domain];
        }

        public void ClearUsedLargeFilesCacheForDomain(Domain domain) {
            _domainToUsedLargeFilesMap[domain].Dispose();
            _domainToUsedLargeFilesMap.Remove(domain);
        }

        public void Initialize() {
            LoadFileData();
            FillLargeFilesDatasOccupiedStatuses();
            CreateFilesReferencingSlotsEmptyBitmasks();
        }

        void OnNewDomainSet(SceneReference sceneRef) {
            if (sceneRef.Domain.Name == "TitleScreen") {
                OnSaveSlotChange(null);
            }
        }

        public void RemoveUnusedFiles() {
            // Starting from 1 because index 0 is reserved for not set index
            for (int fileIndex = 1; fileIndex < _filesReferencingSlots.Count; fileIndex++) {
                var filesReferencingSlots = _filesReferencingSlots[fileIndex];
                if (filesReferencingSlots.IsCreated) {
                    RemoveFileIfNotUsed(in filesReferencingSlots, fileIndex);
                }
            }
        }

        void LoadFileData() {
            _filesDatas = JsonConvert.DeserializeObject<List<LargeFileData>>(PrefMemory.GetString(LargeFilePlayerPrefsKey, "[]"));
            if (_filesDatas?.Count == 0) {
                _filesDatas = JsonConvert.DeserializeObject<List<LargeFileData>>(PlayerPrefs.GetString(LargeFilePlayerPrefsKey, "[]"));
            }
        }

        void SaveFilesDatas() {
            var fileDatasObjString = _filesDatas != null ? JsonConvert.SerializeObject(_filesDatas) : "[]";
            PrefMemory.Set(LargeFilePlayerPrefsKey, fileDatasObjString, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveFileIfNotUsed(in UnsafeSparseBitmask referencingSaveSlotsIndices, int fileIndex) {
            if (referencingSaveSlotsIndices.HasOnes() || _runtimeUsedLargeFilesIndices[(uint)fileIndex]) {
                return;
            }

            RemoveFile(fileIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RemoveFile(int fileIndex) {
            var fileData = _filesDatas[fileIndex];
            if (fileData.IsValid == false) {
                return;
            }

            DeleteFile(fileData.folder, fileData.fileName);
            _filesDatas[fileIndex] = default;
            _filesReferencingSlots[fileIndex].Dispose();
            _filesReferencingSlots[fileIndex] = default;
            _largeFilesDatasOccupiedStatuses.Down((uint)fileIndex);
            SaveFilesDatas();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void GetAndReserveNewFileIndex(out int fileIndex) {
            var freeIndex = _largeFilesDatasOccupiedStatuses.FirstZero();
            if (freeIndex < 1) {
                freeIndex = (int)_largeFilesDatasOccupiedStatuses.ElementsLength;
            }

            _filesDatas.AddToEnsureCount(default, freeIndex + 1);
            _filesReferencingSlots.AddToEnsureCount(default, freeIndex + 1);
            _largeFilesDatasOccupiedStatuses.EnsureCapacity((uint)freeIndex + 1);
            _largeFilesDatasOccupiedStatuses.Up((uint)freeIndex);
            _runtimeUsedLargeFilesIndices.EnsureCapacity((uint)freeIndex + 1);
            _runtimeUsedLargeFilesIndices.Up((uint)freeIndex);

            fileIndex = freeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SaveFile(string folder, byte[] data, LargeFileType type, out int fileIndex) {
            GetAndReserveNewFileIndex(out fileIndex);
            var fileName = LargeFileNameBase + fileIndex;
            _filesDatas[fileIndex] = new LargeFileData(folder, fileName, type);
            _filesReferencingSlots[fileIndex] = new UnsafeSparseBitmask(ARAlloc.Persistent, 1, 1);
            SaveFile(folder, fileName, data);
            SaveFilesDatas();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryLoadFile(int index, out LargeFileData fileData, out byte[] data, LargeFileType typeFilter = LargeFileType.None) {
            if (index >= _filesDatas.Count) {
                data = Array.Empty<byte>();
                fileData = new LargeFileData(string.Empty, string.Empty, LargeFileType.None);
                return false;
            }
            fileData = _filesDatas[index];
            if (typeFilter != LargeFileType.None & fileData.type != typeFilter) {
                data = Array.Empty<byte>();
                return false;
            }

            if (TryLoadFile(fileData.folder, fileData.fileName, out data) == false) {
                data = Array.Empty<byte>();
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void FillLargeFilesDatasOccupiedStatuses() {
            var count = _filesDatas.Count;
            _largeFilesDatasOccupiedStatuses.EnsureCapacity(math.max((uint)count, 1));
            // Starting from 1 because index 0 is reserved for not set index and should always be true
            for (uint i = 1; i < count; i++) {
                _largeFilesDatasOccupiedStatuses[i] = _filesDatas[(int)i].IsValid;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CreateFilesReferencingSlotsEmptyBitmasks() {
            var count = _filesDatas.Count;
            _filesReferencingSlots.Clear();
            _filesReferencingSlots.AddToEnsureCount(default, count);
            // Starting from 1 because index 0 is reserved for not set index and should always be true
            for (int i = 1; i < count; i++) {
                _filesReferencingSlots[i] = new UnsafeSparseBitmask(ARAlloc.Persistent);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SaveFile(string folder, string fileName, byte[] data) {
            SavingWorldMarker.Add(GetSaveFileTask(folder, fileName, data), false);

            static UniTask GetSaveFileTask(string folder, string fileName, byte[] data) {
#if UNITY_PS5
                return UniTask.RunOnThreadPool(() => CloudService.Get.SaveGlobalFile(folder, fileName, data));
#else
                return UniTask.Run(() => CloudService.Get.SaveGlobalFile(folder, fileName, data));
#endif
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryLoadFile(string folder, string fileName, out byte[] data) {
            return CloudService.Get.TryLoadSingleFile(folder, fileName, out data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DeleteFile(string folder, string fileName) {
            CloudService.Get.DeleteGlobalFile(folder, fileName);
        }

#if UNITY_EDITOR
        void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change) {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                foreach ((Domain _, UnsafeBitmask unsafeBitmask) in _domainToUsedLargeFilesMap) {
                    unsafeBitmask.Dispose();
                }
                _domainToUsedLargeFilesMap.Clear();
            }
        }
#endif
    }
}