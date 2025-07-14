using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.CustomSerializers;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Collections;
using Awaken.Utility.CustomSerializers;
using Cysharp.Threading.Tasks;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;
using UniversalProfiling;
using ColorConverter = Awaken.TG.Main.Saving.CustomSerializers.ColorConverter;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Awaken.TG.Main.Saving {
    public class LoadSave {
        // === Constants
        public const BindingFlags DefaultBindingFlags = BindingFlags.Instance |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Public |
                                                        BindingFlags.DeclaredOnly |
                                                        BindingFlags.GetField |
                                                        BindingFlags.GetProperty;

        // strings used in save file
        public const int BufferSize = 8192;
        const int MetaDataPreAllocSize = 8192;

        public static readonly Color LoadSaveProfilerColor = new(0.75f, 0.35f, 0.6f, 1f);
        static readonly UniversalProfilerMarker SaveMarker = new(LoadSaveProfilerColor, "LoadSave.Save");

        public static Encoding Encoding => Encoding.ASCII;

        // === Settings
        public static readonly JsonConverter[] Converters = {
            new ModelConverter(), new ColorConverter(), new RichEnumConverter(), new TemplateConverter(), new Vector2Converter(),
            new Vector3Converter(), new MatrixConverter(), new UnicodeStringConverter(), new QuaternionConverter(),
            new AssetReferenceConverter(), new TextureConverter(), new DictionaryConverter(),
            new FrugalListConverter(), new ItemInSlotsConverter(), new Float2Converter(),
            new LargeFileIndexConverter(), new LargeFilesIndicesConverter(), new UnsafeSparseBitmaskConverter(), new UnsafeBitmaskConverter(),
            new ModelElementsConverter(),
        };

        public static readonly JsonSerializerSettings Settings = new() {
            ContractResolver = new SaveContractResolver(), TypeNameHandling = TypeNameHandling.Auto, Converters = Converters,
            MissingMemberHandling = MissingMemberHandling.Error, Error = OnError, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            NullValueHandling = NullValueHandling.Ignore, ObjectCreationHandling = ObjectCreationHandling.Auto, DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
        };

        // === Fields & Properties
        public static LoadSave Get => s_instance ??= new LoadSave();
        static LoadSave s_instance;

        public readonly JsonSerializer serializer;
        readonly SaveSystem _saveSystem;
        readonly LoadSystem _loadSystem;
        readonly LoadSaveDomainsCache _domainsCache;

        public LoadSystem LoadSystem => _loadSystem;
        public LoadSaveDomainsCache DomainsCache => _domainsCache;

        public static void EDITOR_RuntimeReset() {
            s_instance = null;
        }

        // === Constructor
        LoadSave() {
            serializer = JsonSerializer.Create(Settings);
            _saveSystem = new SaveSystem(this);
            _loadSystem = new LoadSystem();
            _domainsCache = new LoadSaveDomainsCache();

            var domainsSavingVerificationChecker = new CachedDomainsVerificationService();
            domainsSavingVerificationChecker.Init();
            World.Services.Register(domainsSavingVerificationChecker);
        }

        // === Checks
        public bool HeroStateAllowsSave() => World.HasAny<Hero>() && !World.Only<HeroCombat>().IsHeroInFight && !Hero.Current.IsPortaling &&
                                             !Hero.Current.Mounted && Hero.Current.Grounded && !Hero.Current.ShouldDie;

        public bool CanSystemSave(bool checkSavingMarker = true) => !World.HasAny<SaveBlocker>()
                                                                    && HeroStateAllowsSave()
                                                                    && (!checkSavingMarker || !World.HasAny<SavingWorldMarker>());

        public bool CanCacheDomainForSceneChange() => !World.HasAny<SavingWorldMarker>();

        public bool CanPlayerSave(bool checkSavingMarker = true) => CanSystemSave(checkSavingMarker) && !DifficultyRestrictsSave();

        public bool CanQuickSave(bool checkSavingMarker = true) => CanPlayerSave(checkSavingMarker) && UIStateStack.Instance.State.IsMapInteractive;
        public bool CanAutoSave(bool checkSavingMarker = true) => World.Only<AutoSaveSetting>().Enabled && CanPlayerSave(checkSavingMarker) && UIStateStack.Instance.State.IsMapInteractive;
        public bool LoadAllowedInGame() => LoadAllowedInMenu() && UIStateStack.Instance.State.IsMapInteractive;
        public bool LoadAllowedInMenu() => !World.HasAny<SavingWorldMarker>() && !World.HasAny<LoadBlocker>();
        public bool CanContinue() => World.HasAny<SaveSlot>();
        bool DifficultyRestrictsSave() => World.Only<DifficultySetting>().Difficulty.SaveRestriction.HasFlagFast(SaveRestriction.SurvivalSaving) && !World.Services.Get<WyrdnessService>().IsInRepeller(Hero.Current.Coords);

        // === Operations
        public async UniTaskVoid QuickSave() {
            if (!World.HasAny<Hero>()) {
                return;
            }

            SavingWorldMarker.Add(UniTask.DelayFrame(2), true);
            await UniTask.NextFrame();
            if (!CanPlayerSave(false)) {
                return;
            }

            Save(SaveSlot.GetQuickSave(getNewest: false));
        }

        bool TrySerializeMetaDomain(Domain domain, out byte[] serializedData) {
            long domainFileLength;
            bool serializationSuccessful;

            using (var domainWriteStream = new MemoryStream(MetaDataPreAllocSize)) {
                using (var compressionStream = new DeflateStream(domainWriteStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true)) {
                    serializationSuccessful = _saveSystem.Serialize(domain, compressionStream);
                }

                domainWriteStream.Flush();
                domainFileLength = domainWriteStream.Length;

                var success = serializationSuccessful && domainFileLength > 0;

                if (!success) {
                    serializedData = null;
                    return false;
                }

                serializedData = domainWriteStream.ToArray();
                return true;
            }
        }

        public bool TrySerialize(Domain domain) {
            var lfs = World.Services.Get<LargeFilesStorage>();
            lfs.InitializeBeforeSerializingDomain(domain, out var backupData);

            long domainFileLength;
            bool serializationSuccessful;

            using (var domainWriteStream = _domainsCache.GetCachedDomainFileWriteStream(domain)) {
                using (var compressionStream = new DeflateStream(domainWriteStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true)) {
                    serializationSuccessful = _saveSystem.Serialize(domain, compressionStream);
                }
                domainWriteStream.Flush(true);
                domainFileLength = domainWriteStream.Length;
            }

            var success = serializationSuccessful && domainFileLength > 0;

            lfs.AfterDomainSerialized(domain, success, ref backupData);

            if (serializationSuccessful) {
                _domainsCache.SaveDomainFileSizeAndStartVerification(domain, domainFileLength, DomainDataSource.FromGameState);
            } else {
                _domainsCache.RemoveFromCache(domain);
            }

            return success;
        }

        public void Save(SaveSlot saveSlot) {
            using var marker = SaveMarker.Auto();

            if (SavePostpone.ShouldPostpone(saveSlot)) {
                return;
            }
            
            // Delete save slot first, so we don't leave any unnecessary files inside
            var deleteTask = Task.Run(() => {
                CloudService.Get.PrepareSaveSlotForSave(saveSlot.GetDirectory());
            });

            // Make sure Saving World Marker exists
            SavingWorldMarker.Add(UniTask.DelayFrame(1), true);
            var tasks = new List<UniTask>(8);

            foreach (var domain in DomainUtils.SaveSlotDomainsInUse()) {
                TrySerialize(domain);
            }

            long dataSize = _domainsCache.CalculateDataSize();
            
            try {
                deleteTask.GetAwaiter().GetResult();
                _saveSystem.BeginSaving(saveSlot, dataSize);

                var metadataTask = saveSlot.CaptureSlotInfo(_domainsCache.CachedDomainsCount);
                tasks.Add(metadataTask);
                
                var usedLargeFilesIndices = new UnsafeBitmask(1, ARAlloc.Persistent);
                var lfs = World.Services.Get<LargeFilesStorage>();
                
                // Save cached domains that are currently not used but are part of this Game session (think about scenes that were visited long time ago)
                foreach (var domain in _domainsCache.CachedDomains) {
                    if (domain.IsChildOf(Domain.SaveSlot) == false) {
                        continue;
                    }

                    var task = _saveSystem.SaveDomainAsync(domain, saveSlot);
                    UnsafeBitmask.OrWithChangeSize(ref usedLargeFilesIndices, lfs.GetUsedLargeFilesForDomain(domain));
                    tasks.Add(task);
                }

                World.Services.Get<LargeFilesStorage>().SetSaveSlotUsedFilesData(saveSlot.SlotIndex, in usedLargeFilesIndices);
                saveSlot.SetUsedLargeFilesIndices(ref usedLargeFilesIndices);

                Log.Marking?.Warning($"Save {saveSlot}");
            } catch {
                _saveSystem.EndSaving(saveSlot, true).Forget();
                throw;
            }
            
            var endSavingTask = EndSavingTask(tasks, saveSlot);
            SavingWorldMarker.Add(endSavingTask, true);
        }

        async UniTask EndSavingTask(List<UniTask> tasks, SaveSlot saveSlot) {
            await UniTask.WhenAll(tasks);

            var saveSummary = await _saveSystem.EndSaving(saveSlot, false);
            
            var cachedDomainsVerificationService = World.Services.Get<CachedDomainsVerificationService>();
            if (!saveSlot.ValidateDomainAmount(saveSummary, out var errorMessage)) {
                cachedDomainsVerificationService.InformThatSavingCachedDomainFailed(Domain.Gameplay, errorMessage, DomainDataSource.Invalid);
            }
            
            cachedDomainsVerificationService.DiscardSaveIfCorrupted(saveSlot);
        }

        public void SaveMetadataDomainSynchronous(Domain domain) {
            if (TrySerializeMetaDomain(domain, out var data)) {
                _saveSystem.SaveMetadataDomainSynchronous(domain, data);
                PrefMemory.Save();
            }
            Log.Marking?.Warning($"Save {domain.Name}");
        }

        public async UniTask SaveMetadataDomainAsync(Domain domain) {
            if (TrySerializeMetaDomain(domain, out var data)) {
                await _saveSystem.SaveMetaDataDomainAsync(domain, null, data);
            }
            Log.Marking?.Warning($"Save {domain.Name}");
        }

        public void Load(SaveSlot saveSlot, string sourceInfo) {
            if (saveSlot == null || !saveSlot.CanLoad()) {
                return;
            }

            UniversalProfiler.SetMarker(LoadSave.LoadSaveProfilerColor, "LoadSave.StartLoading");
            TitleScreenUtils.PreventRandomCharacterCreatorPreset();
            World.Services.Get<LargeFilesStorage>().OnSaveSlotChange(saveSlot.usedLargeFilesIndices);
            ScenePreloader.Load(saveSlot, sourceInfo);
        }

        public void QuickLoad() {
            Load(SaveSlot.GetQuickSave(allowCreate: false), "Quick Load");
        }

        public void LoadSaveSlotToCache(SaveSlot saveSlot) {
            string relativePath = saveSlot.CurrentDomain.ConstructSavePath(saveSlot);
            var saveSummary = CloudService.Get.BeginLoadDirectory(relativePath);
            if (!saveSlot.ValidateDomainAmount(saveSummary, out string errorMessage)) {
                throw new Exception(errorMessage);
            }
            
            try {
                foreach (var domainToLoad in DomainUtils.GetSaveSlotChildren()) {
                    if (_loadSystem.TryLoadCompressedSaveDataFromFile(domainToLoad, saveSlot, out var compressedBytes)) {
                        long dataWrittenLength;
                        using (Stream cacheStream = _domainsCache.GetCachedDomainFileWriteStream(domainToLoad)) {
                            var bytesRemainingToWrite = compressedBytes.Length;
                            var offset = 0;
                            while (bytesRemainingToWrite > 0) {
                                var bytesToWriteInThisLoopCount = math.min(bytesRemainingToWrite, BufferSize);
                                cacheStream.Write(compressedBytes, offset, bytesToWriteInThisLoopCount);
                                offset += bytesToWriteInThisLoopCount;
                                bytesRemainingToWrite -= bytesToWriteInThisLoopCount;
                            }
                            dataWrittenLength = cacheStream.Length;
                        }
                        _domainsCache.SaveDomainFileSizeAndStartVerification(domainToLoad, dataWrittenLength, DomainDataSource.FromSaveFile);
                    }
                }
            } finally {
                CloudService.Get.EndLoadDirectory(relativePath);
            }
        }

        public bool LoadFromCache(Domain domain) {
            if (_domainsCache.TryGetCachedUncompressedSaveData(domain, out var stream)) {
                _loadSystem.Deserialize(domain, stream);
                stream.Dispose();
                return true;
            }
            return false;
        }

        public void LoadSaveSlotMetadata(Domain domain) {
            if (LoadSystem.TryLoadSingleMetaDataFromFile(domain.ConstructSavePath(null), domain.SaveName, out var stream)) {
                using EventSystem.QueuingHandle eventQueuing = World.EventSystem.EventsQueuing();
                _loadSystem.Deserialize(domain, stream);
                stream.Dispose();
            }
        }

        public void ClearCache(Domain domain) {
            UnityEngine.Pool.ListPool<Domain>.Get(out var cachedDomainsToRemoveFromCache);
            foreach (var cachedDomain in _domainsCache.CachedDomains) {
                if (cachedDomain.IsChildOf(domain, true)) {
                    cachedDomainsToRemoveFromCache.Add(cachedDomain);
                }
            }
            foreach (var domainToRemove in cachedDomainsToRemoveFromCache) {
                _domainsCache.RemoveFromCache(domainToRemove);
                World.Services.Get<LargeFilesStorage>().ClearUsedLargeFilesCacheForDomain(domainToRemove);
            }
            UnityEngine.Pool.ListPool<Domain>.Release(cachedDomainsToRemoveFromCache);
        }

        public void MoveSlotFiles(string oldSlotID, string newSlotID) {
            Domain oldSlotMetaDomain = Domain.SaveSlotMetaData(oldSlotID);
            Domain newSlotMetaDomain = Domain.SaveSlotMetaData(newSlotID);
            ClearCache(oldSlotMetaDomain);

            MoveDomainFile(oldSlotMetaDomain, newSlotMetaDomain, oldSlotID, newSlotID);
            foreach (var d in DomainUtils.GetSaveSlotChildren()) {
                MoveDomainFile(d, d, oldSlotID, newSlotID);
            }
            CloudService.Get.DeleteSaveSlot(oldSlotMetaDomain.ConstructSavePath(oldSlotID));
            PrefMemory.Save();
        }

        public void DeleteSlotFiles(string slotID) {
            Domain slotMetaDomain = Domain.SaveSlotMetaData(slotID);
            ClearCache(slotMetaDomain);
            CloudService.Get.DeleteSaveSlot(slotMetaDomain.ConstructSavePath(slotID));
            PrefMemory.Save();
        }

        public void DeleteAllSaveSlots() {
            var saveSlots = World.All<SaveSlot>().ToArraySlow();
            foreach (SaveSlot slot in saveSlots) {
                slot.Discard();
            }
        }

        void MoveDomainFile(Domain oldDomain, Domain newDomain, string oldSlotID, string newSlotID) {
            string oldPath = oldDomain.ConstructSavePath(oldSlotID);
            string newPath = newDomain.ConstructSavePath(newSlotID);
            string oldFileName = oldDomain.SaveName;
            string newFileName = newDomain.SaveName;
            if (CloudService.Get.TryLoadSingleFile(oldPath, oldFileName, out var bytes)) {
                CloudService.Get.SaveGlobalFile(newPath, newFileName, bytes);
            }
        }

        public static DeflateStream DecompressingSaveStream(byte[] compressedData) {
            var compressedMemoryStream = new MemoryStream(compressedData);
            var uncompressedStream = new DeflateStream(compressedMemoryStream, CompressionMode.Decompress);
            return uncompressedStream;
        }

        // === Error Handling
        public static ErrorsHandlingMode errorHandlingMode = ErrorsHandlingMode.IgnoreErrors;

        static void OnError(object sender, ErrorEventArgs e) {
            if (errorHandlingMode == ErrorsHandlingMode.IgnoreErrors) {
                Debug.LogException(e.ErrorContext.Error);
                e.ErrorContext.Handled = true;
            }
        }

        public enum ErrorsHandlingMode {
            IgnoreErrors = 0,
            AllowErrors = 1,
        }
    }
}