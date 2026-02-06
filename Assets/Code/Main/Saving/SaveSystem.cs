//#define PROFILE_SerializeModel

using System;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Threads;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Threads;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UniversalProfiling;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.Saving {
    public class SaveSystem {
        static readonly UniversalProfilerMarker SerializationMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "SaveSystem.Serialization");
        static readonly UniversalProfilerMarker ServicesSerializationMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "SaveSystem.ServicesSerialization");

        static readonly UniversalProfilerMarker PrepareModelsMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "SaveSystem.PrepareModels");
        static readonly UniversalProfilerMarker SerializeDefinitionsMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "SaveSystem.SerializeDefinitions");
        static readonly UniversalProfilerMarker SerializeModelsMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "SaveSystem.SerializeModels");
        static readonly UniversalProfilerMarker CleanupModelsMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "SaveSystem.CleanupModels");

        // === Fields
        readonly LoadSave _loadSave;

        // === Constructors
        public SaveSystem(LoadSave loadSave) {
            _loadSave = loadSave;
        }

        // === Operations
        public bool Serialize(StructList<Model> allModels, Domain domain, IARStream stream) {
            ThreadSafeUtils.AssertMainThread();

            using var serializationMarker = SerializationMarker.Auto();
            var modelsMask = default(UnsafeBitmask);
            try {
                var context = new SaveWriterContext {
                    domain = domain,
                };
                using var saveWriter = new SaveWriter(stream, context);

                saveWriter.WriteAscii(Application.version);

                bool anySerialized = false;

                modelsMask = new UnsafeBitmask((uint)allModels.Count, Allocator.Temp);
                PrepareModels(allModels, domain, modelsMask);
                SerializeDefinitions(saveWriter, allModels, modelsMask);
                ServicesSerialization(saveWriter, domain, ShouldSaveService, ref anySerialized);
                SerializeModels(saveWriter, allModels, modelsMask, ref anySerialized);

                return anySerialized;
            } catch (Exception e) {
                Log.Important?.Error($"Saving {domain.FullName} failed");
                Debug.LogException(e);
                if (modelsMask.IsCreated) {
                    modelsMask.Dispose();
                }
                return false;
            } finally {
                using (CleanupModelsMarker.Auto()) {
                    // Need to call SerializationEnded after all models are serialized because last model (via relations) can check first model in list if it has "IsBeingSaved" set
                    if (modelsMask.IsCreated) {
                        foreach (var modelIndex in modelsMask.EnumerateOnes()) {
                            allModels[modelIndex].SerializationEnded();
                        }
                        modelsMask.Dispose();
                    } else {
                        foreach (Model model in allModels) {
                            // Cache wasn't created, so we check here again
                            if (model.CurrentDomain == domain) {
                                model.SerializationEnded();
                            }
                        }
                    }
                }
            }
        }

        public void SerializeNpcStash(StructList<Model> allModels, IARStream stream) {
            ThreadSafeUtils.AssertMainThread();

            using var serializationMarker = SerializationMarker.Auto();
            var modelsMask = new UnsafeBitmask((uint)allModels.Count, Allocator.Temp);
            modelsMask.All();
            try {
                var context = new SaveWriterContext {
                    domain = Domain.Gameplay,
                };
                using var saveWriter = new SaveWriter(stream, context);

                saveWriter.WriteAscii(Application.version);

                bool anySerialized = false;

                SerializeDefinitions(saveWriter, allModels, modelsMask);
                saveWriter.WriteType(0);
                SerializeModels(saveWriter, allModels, modelsMask, ref anySerialized);
            } catch (Exception e) {
                Log.Important?.Error("Saving npc stash failed");
                Debug.LogException(e);
            } finally {
                using (CleanupModelsMarker.Auto()) {
                    modelsMask.Dispose();
                    foreach (var model in allModels) {
                        model.SerializationEnded();
                    }
                }
            }
        }
        
        public void SerializeNewGamePlusCache(IARStream stream, StructList<Model> models, UnsafeBitmask modelsMask, Type[] serviceTypesToSave) {
            ThreadSafeUtils.AssertMainThread();
            
            using var serializationMarker = SerializationMarker.Auto();
            
            var context = new SaveWriterContext() {
                domain = Domain.Gameplay,
            };
            
            var lfs = World.Services.Get<LargeFilesStorage>();
            lfs.InitializeBeforeSerializingDomain(context.domain, out var backupData);
            bool serializationFailed = false;
            
            try {
                using var saveWriter = new SaveWriter(stream, context);
                
                bool anySerialized = false;
                
                SerializeDefinitions(saveWriter, models, modelsMask);
                ServicesSerialization(saveWriter, Domain.Gameplay, ShouldSaveNewGamePlusService, ref anySerialized);
                SerializeModels(saveWriter, models, modelsMask, ref anySerialized);
            } catch (Exception e) {
                Log.Important?.Error("Saving New game + cache failed");
                Debug.LogException(e);
                serializationFailed = true;
            } finally {
                using (CleanupModelsMarker.Auto()) {
                    // Need to call SerializationEnded after all models are serialized because last model (via relations) can check first model in list if it has "IsBeingSaved" set
                    foreach (var modelIndex in modelsMask.EnumerateOnes()) {
                        models[modelIndex].SerializationEnded();
                    }
                }
                
                lfs.AfterDomainSerialized(context.domain, !serializationFailed, ref backupData);
            }

            bool ShouldSaveNewGamePlusService(SerializedService s, Domain d) {
                if (!ShouldSaveService(s, d)) {
                    return false;
                }
                foreach (var serviceTypeToSave in serviceTypesToSave) {
                    if (serviceTypeToSave == s.GetType()) {
                        return true;
                    }
                }
                return false;
            }
        }

        static void PrepareModels(StructList<Model> allModels, Domain domain, UnsafeBitmask modelsMask) {
            using (PrepareModelsMarker.Auto()) {
                // We need to prepare all models before serializing them, because some models can depend on others (like parent depends on child elements to save link to them)
                for (var i = 0u; i < allModels.Count; i++) {
                    var model = allModels[i];
                    // Need to check model.HasBeenDiscarded as there is a bug when model throws within Discard so WasDiscarded is false and AllInOrder won't filter it out
                    if (model.CurrentDomain == domain && model.HasBeenDiscarded == false) {
                        modelsMask.Up(i);
                        model.PrepareForSaving();
                    }
                }
            }
        }

        static void SerializeDefinitions(SaveWriter saveWriter, StructList<Model> allModels, UnsafeBitmask modelsMask) {
            using (SerializeDefinitionsMarker.Auto()) {
                foreach (var modelIndex in modelsMask.EnumerateOnes()) {
                    Model model = allModels[modelIndex];

                    // PrepareForSaving set it to true if model can be saved, then after saving we set it to false (via SerializationEnded()), so we don't need to check domain here
                    if (!model.IsBeingSaved) {
                        continue;
                    }

                    saveWriter.WriteType(model.TypeForSerialization);
                    saveWriter.WriteAscii(model.ID);
                }

                saveWriter.WriteType(0);
            }
        }
        
        static void ServicesSerialization(SaveWriter saveWriter, Domain domain, Func<SerializedService, Domain, bool> shouldSaveService, ref bool anySerialized) {
            using (ServicesSerializationMarker.Auto()) {
                // We need to save services after model definitions but before model contents, because service may have references to models and models may need services on its initialization
                foreach (SerializedService service in World.Services.AllSerializedServices()) {
                    if (!shouldSaveService(service, domain)) {
                        continue;
                    }

                    service.OnBeforeSerialize();

                    saveWriter.WriteType(service.TypeForSerialization);
                    saveWriter.WriteStart();
                    service.Serialize(saveWriter);
                    saveWriter.WriteEnd();

                    anySerialized = true;
                }

                saveWriter.WriteType(0);
            }
        }

        static void SerializeModels(SaveWriter saveWriter, StructList<Model> allModels, UnsafeBitmask modelsMask, ref bool anySerialized) {
            using (SerializeModelsMarker.Auto()) {
                foreach (var modelIndex in modelsMask.EnumerateOnes()) {
                    var model = allModels[modelIndex];

                    // PrepareForSaving set it to true if model can be saved, then after saving we set it to false (via SerializationEnded()), so we don't need to check domain here
                    if (!model.IsBeingSaved) {
                        continue;
                    }

                    saveWriter.WriteStart();
                    model.Serialize(saveWriter);
                    saveWriter.WriteEnd();

                    anySerialized = true;
                }
            }
        }

        static bool ShouldSaveService(SerializedService service, Domain domain) {
            if (service is IDomainBoundService domainBound) {
                return domainBound.Domain == domain;
            }

            return domain == SerializedService.DefaultDomain;
        }

        public bool BeginSaving(SaveSlot slot, long size) {
            UniversalProfiler.SetMarker(new Color(0, 1, 1), "SaveSystem.BeginSaving");
            try {
                CloudService.Get.BeginSaveDirectory(slot.GetDirectory(), size);
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
                return false;
            }
        }

        // === Actual saving
        
        /// <summary>
        /// Save all serializable models and services from given domain
        /// </summary>
        public UniTask<bool> SaveDomainAsync(Domain domain, SaveSlot slot) {
            ThreadSafeUtils.AssertMainThread();
            string savePath = domain.ConstructSavePath(slot);
            var cachedDomainFilePath = LoadSaveDomainsCache.GetCachedDomainFilePath(domain);
            return UniTask.RunOnThreadPool(SaveDataInternal);

            UniTask<bool> SaveDataInternal() {
                try {
                    var fileName = domain.SaveName;
                    var compressedData = _loadSave.DomainsCache.TryReadCachedCompressedDomainAsync(domain, cachedDomainFilePath).GetValueOrThrow("Can't load save data");
                    EditorUncompressedDataSave(savePath, fileName, compressedData);
                    CloudService.Get.SaveSlotFile(savePath, fileName, compressedData);
                } catch (Exception e) {
                    MainThreadDispatcher.InvokeAsync(() => { Debug.LogException(e); });
                    return UniTask.FromResult(false);
                }

                return UniTask.FromResult(true);
            }
        }

        public void SaveMetadataDomainSynchronous(Domain domain, byte[] compressedData) {
            ThreadSafeUtils.AssertMainThread();
            try {
                string path = domain.ConstructSavePath(null);
                if (compressedData != null && compressedData.Length != 0) {
                    string fileName = domain.SaveName;

                    EditorUncompressedDataSave(path, fileName, compressedData);
                    CloudService.Get.SaveGlobalFile(path, fileName, compressedData);
                }
            } catch (Exception e) {
                Log.Important?.Error("Saving metadata failed");
                Debug.LogException(e);
                if (!World.HasAny<PopupUI>()) {
                    PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.SavingFailed.Translate());
                }
            }
        }

        public UniTask<bool> SaveMetaDataDomainAsync(Domain domain, SaveSlot slot, byte[] compressedData) {
            string savePath = domain.ConstructSavePath(slot);
            return UniTask.RunOnThreadPool(SaveDataInternal);

            UniTask<bool> SaveDataInternal() {
                try {
                    var fileName = domain.SaveName;
                    EditorUncompressedDataSave(savePath, fileName, compressedData);
                    CloudService.Get.SaveSlotFile(savePath, fileName, compressedData);
                    return UniTask.FromResult(true);
                } catch (Exception e) {
                    MainThreadDispatcher.InvokeAsync(() => { Debug.LogException(e); });
                    return UniTask.FromResult(false);
                }
            }
        }

        static void EditorUncompressedDataSave(string path, string fileName, byte[] compressedData) {
            // Save uncompressed data for debugging purposes
#if (UNITY_EDITOR || DEBUG || AR_DEBUG) && !UNITY_GAMECORE && !UNITY_PS5
            {
                using var decompressingMemoryStream = LoadSave.DecompressingSaveStream(compressedData);
                using var uncompressedMemoryStream = new MemoryStream();
                decompressingMemoryStream.CopyTo(uncompressedMemoryStream);
                var uncompressedData = uncompressedMemoryStream.ToArray();
                CloudService.Get.SaveSlotFile(path, fileName + LoadSystem.UncompressedFileSuffix, uncompressedData);
            }
#endif
        }

        public static void DispatchSpawnSavingFailedPopup() {
            MainThreadDispatcher.InvokeAsync(() => {
                if (World.HasAny<PopupUI>()) {
                    return;
                }

                PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.SavingFailed.Translate());
            });
        }
    }
}