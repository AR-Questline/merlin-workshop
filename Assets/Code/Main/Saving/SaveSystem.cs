//#define PROFILE_SerializeModel

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Threads;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.Threads;
using Cysharp.Threading.Tasks;
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
        public bool Serialize(Domain domain, Stream stream) {
            ThreadSafeUtils.AssertMainThread();

            using var serializationMarker = SerializationMarker.Auto();

            var allModels = World.AllInOrder();
            try {
                var context = new SaveWriterContext {
                    domain = domain,
                };
                using var saveWriter = new SaveWriter(stream, context);

                saveWriter.WriteAscii(Application.version);

                bool anySerialized = false;


                using (PrepareModelsMarker.Auto()) {
                    // We need to prepare all models before serializing them, because some models can depend on others (like parent depends on child elements to save link to them)
                    foreach (Model model in allModels) {
                        // Need to check model.HasBeenDiscarded as there is a bug when model throws within Discard so WasDiscarded is false and AllInOrder won't filter it out
                        if (model.CurrentDomain == domain && model.HasBeenDiscarded == false) {
                            model.PrepareForSaving();
                        }
                    }
                }

                using (SerializeDefinitionsMarker.Auto()) {
                    foreach (Model model in allModels) {
                        // PrepareForSaving set it to true if model can be saved, then after saving we set it to false (via SerializationEnded()), so we don't need to check domain here
                        if (!model.IsBeingSaved) {
                            continue;
                        }

                        saveWriter.WriteType(model.TypeForSerialization);
                        saveWriter.WriteAscii(model.ID);
                    }

                    saveWriter.WriteType(0);
                }

                using (ServicesSerializationMarker.Auto()) {
                    // We need to save services after model definitions but before model contents, because service may have references to models and models may need services on its initialization
                    foreach (SerializedService service in World.Services.AllSerializedServices()) {
                        if (!ShouldSaveService(service, domain)) {
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

                using (SerializeModelsMarker.Auto()) {
                    foreach (Model model in allModels) {
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

                return anySerialized;
            } catch (Exception e) {
                Log.Important?.Error($"Saving {domain.FullName} failed");
                Debug.LogException(e);

                return false;
            } finally {
                using (CleanupModelsMarker.Auto()) {
                    // Need to call SerializationEnded after all models are serialized because last model (via relations) can check first model in list if it has "IsBeingSaved" set
                    foreach (Model model in allModels) {
                        // We don't cache which models to save as we don't want to make such allocations, so we check here again
                        if (model.CurrentDomain == domain) {
                            model.SerializationEnded();
                        }
                    }
                }
            }
        }

        bool ShouldSaveService(SerializedService service, Domain domain) {
            if (service is IDomainBoundService domainBound) {
                return domainBound.Domain == domain;
            }

            return domain == SerializedService.DefaultDomain;
        }

        public void BeginSaving(SaveSlot slot, long size) {
            UniversalProfiler.SetMarker(new Color(0, 1, 1), "SaveSystem.BeginSaving");
            CloudService.Get.BeginSaveDirectory(slot.GetDirectory(), size);
        }

        public async UniTask EndSaving(SaveSlot slot, bool failed) {
            bool success = await CloudService.Get.EndSaveDirectory(slot.GetDirectory(), failed);

            if (!success) {
                slot.Discard();
                if (!World.HasAny<PopupUI>()) {
                    PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.SavingFailed.Translate());
                }
            } else if (World.Only<DifficultySetting>().Difficulty.SaveRestriction.HasFlagFast(SaveRestriction.OneSaveSlot)) {
                World.All<SaveSlot>().Where(s => s.HeroId == slot.HeroId && s != slot).ToArray().ForEach(s => s.Discard());
            }

            UniversalProfiler.SetMarker(new Color(0, 1, 1), "SaveSystem.EndSaving");
        }
        
        // === Actual saving
        
        /// <summary>
        /// Save all serializable models and services from given domain
        /// </summary>
        public UniTask SaveDomainAsync(Domain domain, SaveSlot slot) {
            ThreadSafeUtils.AssertMainThread();
            string savePath = domain.ConstructSavePath(slot);
            var cachedDomainFilePath = LoadSaveDomainsCache.GetCachedDomainFilePath(domain);
            try {
                return UniTask.RunOnThreadPool(SaveDataInternal);
            } catch (Exception e) {
                Log.Important?.Error("Saving failed");
                Debug.LogException(e);
            }
            return UniTask.CompletedTask;
            
            UniTask SaveDataInternal() {
                try {
                    var fileName = domain.SaveName;
                    var compressedData = _loadSave.DomainsCache.TryReadCachedCompressedDomainAsync(domain, cachedDomainFilePath).GetValueOrThrow("Can't load save data");
                    EditorUncompressedDataSave(savePath, fileName, compressedData);
                    CloudService.Get.SaveSlotFile(savePath, fileName, compressedData);
                } catch (Exception e) {
                    MainThreadDispatcher.InvokeAsync(() => {
                        Debug.LogException(e);
                        if (World.HasAny<PopupUI>()) {
                            return;
                        }
                        PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.SavingFailed.Translate());
                    });
                }

                return UniTask.CompletedTask;
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
        
        public UniTask SaveMetaDataDomainAsync(Domain domain, SaveSlot slot, byte[] compressedData) {
            string savePath = domain.ConstructSavePath(slot);
            try {
                return UniTask.RunOnThreadPool(SaveDataInternal);
            } catch (Exception e) {
                Log.Important?.Error("Saving metadata failed");
                Debug.LogException(e);
            }
            return UniTask.CompletedTask;

            UniTask SaveDataInternal() {
                try {
                    var fileName = domain.SaveName;
                    EditorUncompressedDataSave(savePath, fileName, compressedData);
                    CloudService.Get.SaveSlotFile(savePath, fileName, compressedData);
                } catch (Exception e) {
                    MainThreadDispatcher.InvokeAsync(() => {
                        if (World.HasAny<PopupUI>()) {
                            return;
                        }
                        Debug.LogException(e);
                        PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.SavingFailed.Translate());
                    });

                }

                return UniTask.CompletedTask;
            }
        }

        static void EditorUncompressedDataSave(string path, string fileName, byte[] compressedData) {
            // Save uncompressed data for debugging purposes
#if UNITY_EDITOR || DEBUG || AR_DEBUG
            {
                using var decompressingMemoryStream = LoadSave.DecompressingSaveStream(compressedData);
                using var uncompressedMemoryStream = new MemoryStream();
                decompressingMemoryStream.CopyTo(uncompressedMemoryStream);
                var uncompressedData = uncompressedMemoryStream.ToArray();
                CloudService.Get.SaveSlotFile(path, fileName + "_uncompressed", uncompressedData);
            }
#endif
        }
    }
}