using System;
using System.IO;
using Awaken.Utility;
using Awaken.Utility.LowLevel;
using QFSW.QC;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Assets.ShadersPreloading {
    public class ShadersTracer {
        const string InBuildFolderName = "ShadersTraces";
        const string InEditorFolderPath = "SharedAssets/ShadersTraces";
        const string FileNamePrefix = "ShadersTrace";
        const string AutomaticShadersTracingKeyword = "automatic_shaders_tracing";
        const float AutomaticSaveDelayInSeconds = 30f;

        bool _autoStartTracing;
        GraphicsStateCollection _activeCollection;
        float _timeElapsed;

        public static ShadersTracer Instance { get; private set; }

        // === Lifetime
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init() {
            Instance = new ShadersTracer();
        }

        ShadersTracer() {
#if DEBUG
            _autoStartTracing = Configuration.GetBool(AutomaticShadersTracingKeyword, _autoStartTracing);
            PlayerLoopUtils.RemoveFromPlayerLoop<ShadersTracer, Update>();
            PlayerLoopUtils.RegisterToPlayerLoopEnd<ShadersTracer, Update>(Update);

            Application.wantsToQuit += WantsToQuit;
#else
            _autoStartTracing = false;
#endif
            if (_autoStartTracing) {
                StartTracing();
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        void Update() {
            if (_activeCollection != null && _activeCollection.isTracing) {
                _timeElapsed += Time.deltaTime;
                if (_timeElapsed > AutomaticSaveDelayInSeconds) {
                    _timeElapsed = 0;
                    StopTracing();
                    SaveTraceToFile();
                    StartTracing();
                }
            }
        }

        bool WantsToQuit() {
            if (_autoStartTracing) {
                StopTracing();
                SaveTraceToFile();
            }
            return true;
        }

        // === Tracing
        void StartTracing() {
#if DEBUG
            EnsureActiveCollectionSet();
            if (_activeCollection.isTracing == false) {
                bool startedTracing = _activeCollection.BeginTrace();
                if (startedTracing) {
                    Log.Debug?.Info($"Shaders tracing started. Graphics api: {SystemInfo.graphicsDeviceType}");
                } else {
                    Log.Important?.Error($"Shaders tracing could not be started. SupportsParallelPSOCreation: {SystemInfo.supportsParallelPSOCreation}. Graphics api: {SystemInfo.graphicsDeviceType}");
                }
            }
#else
            Log.Critical?.Error("Shaders tracing is supported only in DEBUG build");
#endif
        }

        void StopTracing() {
#if DEBUG
            if (_activeCollection == null) {
                Log.Important?.Error($"{nameof(ShadersTracer)} is not initialized");
                return;
            }
            if (_activeCollection.isTracing) {
                _activeCollection.EndTrace();
            }
#else
            Log.Critical?.Error("Shaders tracing is supported only in DEBUG build");
#endif
        }

        void SaveTraceToFile() {
#if DEBUG
            if (_activeCollection == null) {
                Log.Important?.Error($"{nameof(ShadersTracer)} is not initialized");
                return;
            }
            var pathForBuild = GetOutputFilePathForBuild(_activeCollection);
            Log.Debug?.Info($"Saving shaders trace in path: {pathForBuild}. Graphics states count: {_activeCollection.totalGraphicsStateCount}");
            try {
                var directoryPathForBuild = GetOutputFileDirectoryPathForBuild();
                if (Directory.Exists(directoryPathForBuild) == false) {
                    Directory.CreateDirectory(directoryPathForBuild);
                }
                _activeCollection.SaveToFile(pathForBuild);
            } catch (Exception e) {
                Debug.LogException(e);
            }
#else
            Log.Critical?.Error("Shaders tracing is supported only in DEBUG build");
#endif
        }

        // === Others
        void EnsureActiveCollectionSet() {
            if (_activeCollection != null) {
                return;
            }
            var platform = Application.platform;
            var graphicsDeviceType = SystemInfo.graphicsDeviceType;
            if (ShadersPreloadingCommon.TryFindMatchingCollectionInStreamingAssets(platform, graphicsDeviceType, out _activeCollection, true) == false) {
                _activeCollection = new GraphicsStateCollection();
                _activeCollection.name = ShadersPreloadingCommon.GetFileName(platform, graphicsDeviceType);
                _activeCollection.runtimePlatform = platform;
                _activeCollection.graphicsDeviceType = graphicsDeviceType;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
            if (scene.name == "TitleScreen") {
                SaveTracingResultsToFileCommand();
            }
        }

        static string GetOutputFileDirectoryPathForBuild() => Path.Combine(Application.persistentDataPath, InBuildFolderName);

        static string GetOutputFilePathForBuild(GraphicsStateCollection collection) {
            return Path.Combine(Application.persistentDataPath, InBuildFolderName, GetOutputFileName(collection));
        }

        static string GetOutputFilePathForEditor(GraphicsStateCollection collection) {
            return Path.Combine(InEditorFolderPath, GetOutputFileName(collection, ""));
        }

        static string GetOutputFileName(GraphicsStateCollection collection, string extension = ".graphicsstate") {
            var fileName = string.Concat(FileNamePrefix, "_", collection.runtimePlatform.ToString(), "_", collection.graphicsDeviceType.ToString(), extension);
            return fileName;
        }

        // === Commands
        [Command("shadersTracer.startTracing", "")]
        public static void StartTracingCommand() {
            Instance.StartTracing();
        }

        [Command("shadersTracer.stopTracing", "")]
        static void StopTracingCommand() {
            Instance.StopTracing();
        }

        [Command("shadersTracer.saveToFile", "If shaders tracer was tracing, writes traced data to file")]
        public static void SaveTracingResultsToFileCommand() {
            if (Instance == null) {
                Log.Debug?.Error($"No {nameof(ShadersTracer)} instance");
                return;
            }
            if (Instance._activeCollection == null) {
                Log.Debug?.Error($"{nameof(ShadersTracer)} has no active collection");
                return;
            }
            var wasTracing = Instance._activeCollection.isTracing;
            Instance.StopTracing();
            Instance.SaveTraceToFile();
            if (wasTracing) {
                Instance.StartTracing();
            }
        }
    }
}