using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Awaken.TG.Main.Saving.Cloud.Services;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.Patchers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Serialization;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;
using UniversalProfiling;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.Saving {
    public class LoadSystem {
        public const string UncompressedFileSuffix = "_uncompressed";
        
        public static bool IsLoadingDifferentVersion => LoadSave.Get.LoadSystem._versionString != Application.version;
        static readonly UniversalProfilerMarker LoadToWorldMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "LoadSystem.LoadToWorld");
        static readonly UniversalProfilerMarker LoadModelDefinitionsMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "LoadSystem.LoadModelDefinitions");
        static readonly UniversalProfilerMarker LoadModelContentsMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "LoadSystem.LoadModelContents");
        static readonly UniversalProfilerMarker LoadServicesMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "LoadSystem.LoadServices");
        static readonly UniversalProfilerMarker RestoreWorldMarker = new UniversalProfilerMarker(LoadSave.LoadSaveProfilerColor, "LoadSystem.RestoreWorld");

        // === Fields & Properties
        string _versionString;

        bool _isRestoring;
        Action _afterGameRestored;
        Dictionary<string, ConstructorInfo> _typeStringToTypeConstructorMap;
        PatcherService Patcher => World.Services.Get<PatcherService>();

        // === Operations
        public void AfterGameRestored(Action action) {
            if (_isRestoring) {
                _afterGameRestored += action;
            } else {
                action.Invoke();
            }
        }

        public void Deserialize(Domain domain, Stream stream, out List<Model> models) {
            using var restoringScope = new RestoringScope(this);
            using var loadToWorldMarker = LoadToWorldMarker.Auto();
            
            var context = new SaveReaderContext {
                deserializedModels = new Dictionary<string, Model>(1024),
            };
            using var saveReader = new SaveReader(stream, context);

            saveReader.ReadAscii(out _versionString);
            var version = new Version(_versionString);
            if (_versionString != Application.version) {
                Log.Marking?.Warning($"Loading save '{domain.Name}' from version '{_versionString}'");
            }

            models = new List<Model>(1024);
            
            LoadModelDefinitions(saveReader, models, context);
            LoadServices(saveReader, Patcher, version);
            LoadModelContents(saveReader, models, Patcher, version, context);
            RestoreWorld(models, domain);

            Patcher.AfterRestorePatch(version);
            restoringScope.MarkProperlyRestored();
        }

        public void DeserializeNewGamePlusCache(Stream stream, Domain domain) {
            using var restoringScope = new RestoringScope(this);
            using var loadToWorldMarker = LoadToWorldMarker.Auto();
            
            var context = new SaveReaderContext {
                deserializedModels = new Dictionary<string, Model>(1024),
            };
            using var saveReader = new SaveReader(stream, context);

            var models = new List<Model>(1024);
            
            LoadModelDefinitions(saveReader, models, context);
            LoadServices(saveReader, null, null);
            LoadModelContents(saveReader, models, null, null, context);
            RestoreWorld(models, domain);
            
            restoringScope.MarkProperlyRestored();
        }
        
        public void DeserializeWithoutRestore(Domain domain, Stream stream, Action<Model> modelAction, Action<SerializedService> serviceAction) {
            using var restoringScope = new RestoringScope(this);
            using var loadToWorldMarker = LoadToWorldMarker.Auto();
            
            var context = new SaveReaderContext {
                deserializedModels = new Dictionary<string, Model>(1024),
            };
            using var saveReader = new SaveReader(stream, context);
            
            saveReader.ReadAscii(out _versionString);
            var version = new Version(_versionString);
            if (_versionString != Application.version) {
                Log.Marking?.Warning($"Loading save '{domain.Name}' from version '{_versionString}'");
            }
            
            var models = new List<Model>(1024);
            var services = new List<SerializedService>(64);
            
            LoadModelDefinitions(saveReader, models, context);
            LoadAndGetServices(saveReader, services, Patcher, version);
            foreach (var service in services) {
                serviceAction?.Invoke(service);
            }
            LoadModelContents(saveReader, models, Patcher, version, context);
            foreach (var model in models) {
                modelAction?.Invoke(model);
            }

            foreach (var model in models) {
                World.EventSystem.RemoveAllListenersOwnedBy(model, true);
                World.EventSystem.RemoveAllListenersTiedTo(model, true);
            }
            
            restoringScope.MarkProperlyRestored();
        }

        static void LoadModelDefinitions(SaveReader saveReader, List<Model> models, SaveReaderContext context) {
            using (LoadModelDefinitionsMarker.Auto()) {
                while (saveReader.TryReadValidType(out var type)) {
                    saveReader.ReadAscii(out var id);
                    var model = ModelTyper.CreateForDeserialization(type);
                    models.Add(model);
                    if (model == null) {
                        continue;
                    }
                    model.AssignID(id);
                    model.EnsureIdIsValid();
                    context.deserializedModels[id] = model;
                }
            }
        }

        static void LoadServices(SaveReader saveReader, [CanBeNull] PatcherService patcher, [CanBeNull] Version version) {
            using (LoadServicesMarker.Auto()) {
                while (saveReader.TryReadValidType(out var type)) {
                    var service = ServiceTyper.CreateForDeserialization(type);
                    if (service == null) {
                        saveReader.ReadToEnd();
                        continue;
                    }
                    saveReader.ReadStart();
                    while (saveReader.TryReadName(out var name)) {
                        service.Deserialize(name, saveReader);
                        saveReader.ReadToSeparator();
                    }
                    patcher?.AfterDeserializedService(version, service);
                    service.OnAfterDeserialize();
                }
            }
        }
        
        static void LoadAndGetServices(SaveReader saveReader, List<SerializedService> services, [CanBeNull] PatcherService patcher, [CanBeNull] Version version) {
            using (LoadServicesMarker.Auto()) {
                while (saveReader.TryReadValidType(out var type)) {
                    var service = ServiceTyper.CreateForDeserialization(type);
                    if (service == null) {
                        saveReader.ReadToEnd();
                        continue;
                    }
                    services.Add(service);
                    saveReader.ReadStart();
                    while (saveReader.TryReadName(out var name)) {
                        service.Deserialize(name, saveReader);
                        saveReader.ReadToSeparator();
                    }
                    patcher?.AfterDeserializedService(version, service);
                    service.OnAfterDeserialize();
                }
            }
        }

        static void LoadModelContents(SaveReader saveReader, List<Model> models, [CanBeNull] PatcherService patcher, [CanBeNull] Version version, SaveReaderContext context) {
            using (LoadModelContentsMarker.Auto()) {
                for (int i = 0; i < models.Count; i++) {
                    var model = models[i];
                    saveReader.ReadStart();
                    if (model == null) {
                        saveReader.ReadToEnd();
                        models.RemoveAt(i--);
                        continue;
                    }
                    patcher?.BeforeDeserializedModel(version, model);
                    while (saveReader.TryReadName(out var name)) {
                        model.Deserialize(name, saveReader);
                        saveReader.ReadToSeparator();
                    }
                    
                    if (patcher?.AfterDeserializedModel(version, model) == false) {
                        model.DeserializationFailed();
                        models.RemoveAt(i--);
                        context.deserializedModels.Remove(model.ID);
                    } else {
                        model.AfterDeserialize();
                    }
                }
                // Cleanup models that were removed by the patcher
                RemoveInvalidModels(models);

                for (int i = 0; i < models.Count; i++) {
                    var model = models[i];
                    if (!model.WasDiscarded) {
                        model.PreRestore();
                    }
                }
                
                RemoveInvalidModels(models);
            }
        }

        static void RestoreWorld(List<Model> models, Domain domain) {
            using (RestoreWorldMarker.Auto()) {
                try {
                    foreach (var model in models) {
                        if (model.WasDiscarded) {
                            continue;
                        }

                        try {
                            model.RevalidateElements();
                            model.RevalidateRelations();
                        } catch (Exception e) {
                            Log.Critical?.Error(
                                $"Exception on World.Validate happened for Model {LogUtils.GetDebugName(model)}");
                            AutoBugReporting.SendAutoReport("World.Validate Exception",
                                $"Exception happened for Model {LogUtils.GetDebugName(model)}\n{e}");
                            throw;
                        }

                        model.SetDomain(domain);
                        
                        try {
                            World.Register(model, ModelUtils.ModelHierarchyTypes(model));
                            World.Restore(model);
                        } catch (Exception e) {
                            Log.Critical?.Error($"Exception on World.Restore happened for Model {LogUtils.GetDebugName(model)}");
                            AutoBugReporting.SendAutoReport("World.Restore Exception", $"Exception happened for Model {LogUtils.GetDebugName(model)}\n{e}");
                            throw;
                        }
                    }

                    var stackToFullyInitialize = new Stack<Model>();
                    foreach (var model in models) {
                        if (model.WasDiscarded) {
                            continue;
                        }

                        var parent = model is Element element ? (Model)element.GenericParentModel : null;
                        while (stackToFullyInitialize.TryPeek(out var toFullyInitialize) && toFullyInitialize != parent) {
                            stackToFullyInitialize.Pop();
                            toFullyInitialize.MarkAsFullyInitialized();
                            toFullyInitialize.StoppedInitialization();
                        }

                        stackToFullyInitialize.Push(model);
                    }

                    while (stackToFullyInitialize.TryPop(out var toFullyInitialize)) {
                        toFullyInitialize.MarkAsFullyInitialized();
                        toFullyInitialize.StoppedInitialization();
                    }

                    foreach (var model in models) {
                        if (!model.WasDiscarded) {
                            model.AfterWorldRestored();
                        }
                    }
                } catch {
                    TitleScreen.wasLoadingFailed = LoadingFailed.ModelInitialization;
                    foreach (var model in models) {
                        if (!model.WasDiscarded) {
                            try {
                                model.StoppedInitialization();
                            } catch (Exception e) {
                                Log.Critical?.Error($"Exception below happened on World.StoppedInitialization for Model {LogUtils.GetDebugName(model)}");
                                Debug.LogException(e);
                            }
                        }
                    }
                    throw;
                }
            }
        }

        static void RemoveInvalidModels(List<Model> models) {
            bool anyRemoved = true;
            var sb = new StringBuilder();
            while (anyRemoved) {
                anyRemoved = false;
                for (int i = 0; i < models.Count; i++) {
                    var model = models[i];
                    bool isValid = true;
                    if (model.IsValidAfterLoad() == false) {
                        sb.Append($"Disposed mutilated model ").AppendLine(LogUtils.GetDebugName(model));
                        isValid = false;
                    } else if (model is Element { GenericParentModel: null or { WasDiscarded: true } }) {
                        sb.Append("Disposed orphaned element ").AppendLine(LogUtils.GetDebugName(model));
                        isValid = false;
                    }

                    if (isValid == false) {
                        model.DeserializationFailed();
                        models.RemoveAt(i--);
                        anyRemoved = true;
                    }
                }
            }
            if (sb.Length > 0) {
                Log.Important?.Error(sb.ToString(), logOption: LogOption.NoStacktrace);
            }
        }

        // === Helpers
        public static bool TryLoadSingleMetaDataFromFile(string path, string fileName, out Stream stream) {
#if UNITY_EDITOR
            try {
                var uncompressedName = fileName + UncompressedFileSuffix;
                byte[] uncompressedData;
                bool resultUncompressed = CloudService.Get.TryLoadSingleFile(path, uncompressedName, out uncompressedData);
                if (resultUncompressed) {
                    Log.Debug?.Info($"Loaded uncompressed json from {uncompressedName}");
                    stream = new MemoryStream(uncompressedData);
                    return true;
                }
            } catch {
                // ignore
            }
#endif
            byte[] compressedData;
            bool result = CloudService.Get.TryLoadSingleFile(path, fileName, out compressedData);
            stream = result ? LoadSave.DecompressingSaveStream(compressedData) : null;
            return result;
        }

        static bool TryLoadCompressedSaveDataFromFile(string path, string fileName, out byte[] compressedSaveData) {
#if UNITY_EDITOR
            try {
                var uncompressedName = fileName + UncompressedFileSuffix;
                byte[] uncompressedData;
                bool resultUncompressed = CloudService.Get.TryLoadSaveSlotFile(path, uncompressedName, out uncompressedData);

                if (resultUncompressed) {
                    Log.Debug?.Info($"Loaded uncompressed json from {uncompressedName}");
                    using var memoryStream = new MemoryStream(uncompressedData);
                    using var compressingStream = new DeflateStream(memoryStream, CompressionLevel.Optimal);
                    using var compressedDataStream = new MemoryStream();
                    compressingStream.CopyTo(compressedDataStream);
                    compressedSaveData = compressedDataStream.ToArray();
                    return true;
                }
            } catch {
                // Ignore
            }
#endif

            // load compressed json
            bool result = CloudService.Get.TryLoadSaveSlotFile(path, fileName, out compressedSaveData);
            return result;
        }

        public bool TryLoadCompressedSaveDataFromFile(Domain domain, [CanBeNull] SaveSlot saveSlot, out byte[] compressedData) {
            string path = domain.ConstructSavePath(saveSlot);
            string fileName = domain.SaveName;
            return TryLoadCompressedSaveDataFromFile(path, fileName, out compressedData);
        }
        
        struct RestoringScope : IDisposable {
            readonly LoadSystem _loadSystem;
            bool _properlyRestored;

            public RestoringScope(LoadSystem loadSystem) {
                _loadSystem = loadSystem;
                _properlyRestored = false;

                _loadSystem._isRestoring = true;
            }

            public void MarkProperlyRestored() {
                _properlyRestored = true;
            }

            public void Dispose() {
                _loadSystem._isRestoring = false;
                if (_properlyRestored & _loadSystem._afterGameRestored != null) {
                    _loadSystem._afterGameRestored.Invoke();
                }
                _loadSystem._afterGameRestored = null;
            }
        }
        
#if UNITY_EDITOR
        public static bool EDITOR_TryLoadCompressedSaveDataFromFile(string path, string fileName, out byte[] compressedSaveData) {
            return TryLoadCompressedSaveDataFromFile(path, fileName, out compressedSaveData);
        }
#endif
    }
}