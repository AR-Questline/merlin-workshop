using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.Timing;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.MVC.Domains {
    /// <summary>
    /// Service used for all operations on scenes, encapsulates scenes-addressables logic
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public class SceneService : IService {
        public const string VisitedSceneContextName = "VisitedScenes";
        public const string ScenesLabel = "scene";
        public const string ScenesGroup = "Scenes";
        public const string ScenesEditorGroup = "ScenesEditor";
        public const string StaticSceneSuffix = "_Static";
        const string TestArenaSceneSuffix = "TestArena";

        CancellationTokenSource _mainSceneExistenceTokenSource;
        CancellationTokenSource _additiveSceneExistenceTokenSource;

        public CancellationToken ActiveSceneExistenceToken {
            get {
                if (AdditiveSceneRef != null) {
                    return _additiveSceneExistenceTokenSource?.Token ?? default;
                }

                return _mainSceneExistenceTokenSource?.Token ?? default;
            }
        }

        // === Properties
        public SceneReference MainSceneRef { get; private set; }
        public SceneReference AdditiveSceneRef { get; private set; }
        public IMapScene MainSceneBehaviour { get; private set; }
        public IMapScene AdditiveSceneBehaviour { get; private set; }
        public Domain ActiveDomain { get; private set; }
        public ARTimeSpan ActiveSceneLoadTime { get; private set; }

        public bool IsOpenWorld { get; private set; }
        public bool IsTestArena => ActiveSceneRef.Name.Contains(TestArenaSceneSuffix);
        public Domain MainDomain => Domain.Scene(MainSceneRef);
        public SceneReference ActiveSceneRef => AdditiveSceneRef ?? MainSceneRef;
        public IMapScene ActiveSceneBehaviour => AdditiveSceneBehaviour ?? MainSceneBehaviour;
        public string ActiveSceneDisplayName => LocTerms.GetSceneName(ActiveSceneRef);
        public bool IsAdditiveScene => AdditiveSceneRef is not null;
        public bool IsPrologue { get; private set; }

        public async UniTask InitAllSceneReferences() {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(ScenesLabel);
            var locations = await locationsHandle.Task;
            s_allSceneDomains = new Domain[locations.Count];
            for (int i = 0; i < locations.Count; i++) {
                s_allSceneDomains[i] = SceneReference.ByName(locations[i].PrimaryKey).Domain;
            }
        }

        // === Static scene loading
        static readonly Dictionary<string, SceneLoadOperation> HandleByName = new();
        static Domain[] s_allSceneDomains;

        public static void EDITOR_RuntimeReset() {
            HandleByName.Clear();
            s_allSceneDomains = null;
        }

        public static SceneLoadOperation LoadSceneAsync(SceneReference sceneRef, LoadSceneMode sceneMode = LoadSceneMode.Single) {
            string name = sceneRef.Name;
            if (HandleByName.ContainsKey(name)) {
                throw new InvalidOperationException($"Loading scene that has already been loaded. Scene name: {name}");
            }

            if (World.Services != null) {
                bool apv = World.Services.Get<CommonReferences>().SceneConfigs.HasApvEnabled(sceneRef);
                World.Any<GeneralGraphicsWithAPV>()?.RefreshQualityRuntime(apv);
            }

            var asyncOperationHandle = Addressables.LoadSceneAsync(name, sceneMode);
            SceneLoadOperation loadOperation = new(name, asyncOperationHandle, isUnload: false);
#if UNITY_EDITOR && !SCENES_PROCESSED
            loadOperation.OnComplete(() => OnSceneLoaded(name));
#endif
            HandleByName[name] = loadOperation;
            return loadOperation;
        }

        public static SceneLoadOperation UnloadSceneAsync(SceneReference sceneRef) {
            string name = sceneRef.Name;
            var asyncOperationHandle = Addressables.UnloadSceneAsync(HandleByName[name].handle);
            HandleByName.Remove(name);
#if UNITY_EDITOR && !SCENES_PROCESSED
            ResetSceneProcessedStatusWithReflection(asyncOperationHandle.Result.Scene);
#endif
            return new(name, asyncOperationHandle, isUnload: true);
        }

        public static Domain[] AllSceneDomains() {
            return s_allSceneDomains;
        }

        public static void SceneLoaded(SceneReference sceneRef) {
            HandleByName[sceneRef.Name].Complete();
        }

        public static void SceneInitialized(SceneReference sceneRef) {
            HandleByName[sceneRef.Name].Initialize();
        }

        public static void InitLoadBasedBehaviours() {
            foreach (var behaviourWithInit in Object.FindObjectsByType<MonoBehaviourWithInitAfterLoaded>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
                behaviourWithInit.Init();
            }
        }

        public static void InitLoadBasedBehavioursOnScene(Scene scene) {
            foreach (var behaviourWithInit in GameObjects.FindComponentsByTypeInScene<MonoBehaviourWithInitAfterLoaded>(scene, false, 50)) {
                behaviourWithInit.Init();
            }
        }

        // === Operations
        public void ChangeTo(SceneReference sceneReference, bool isAdditive = false) {
            SetLoadTime();
            SetNewMapDomain(sceneReference, isAdditive);
            World.Services.TryGet<GameplayMemory>()?.Context(VisitedSceneContextName).Set(sceneReference.Name, true);
        }

        void SetNewMapDomain(SceneReference sceneReference, bool isAdditive) {
            if (isAdditive) {
                AdditiveSceneRef = sceneReference;
                ActiveDomain = Domain.Scene(sceneReference);
                AdditiveSceneBehaviour = AdditiveSceneRef.RetrieveMapScene();

                _additiveSceneExistenceTokenSource?.Cancel();
                _additiveSceneExistenceTokenSource = new CancellationTokenSource();
            } else {
                AdditiveSceneRef = null;
                AdditiveSceneBehaviour = null;
                bool mainSceneChangeOccured = sceneReference != MainSceneRef;
                if (mainSceneChangeOccured) {
                    MainSceneRef = sceneReference;
                    _mainSceneExistenceTokenSource?.Cancel();
                    _mainSceneExistenceTokenSource = new CancellationTokenSource();
                }

                ActiveDomain = Domain.Scene(sceneReference);
                MainSceneBehaviour = MainSceneRef.RetrieveMapScene();

                _additiveSceneExistenceTokenSource?.Cancel();
                _additiveSceneExistenceTokenSource = null;
            }

            bool isOpenWorld = CommonReferences.Get.SceneConfigs.IsOpenWorld(ActiveSceneRef);
            bool openWorldChanged = isOpenWorld != IsOpenWorld;

            if (openWorldChanged) {
                IsOpenWorld = isOpenWorld;
            }

            IsPrologue = CommonReferences.Get.SceneConfigs.IsPrologue(ActiveSceneRef);

            SceneLifetimeEvents.Get.ValidateDomain(sceneReference, isAdditive);

            if (openWorldChanged) {
                World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.OpenWorldStateChanged, IsOpenWorld);
                foreach (var presence in World.All<NpcPresence>()) {
                    presence.OnWorldStateChanged();
                }
            }
        }

        void SetLoadTime() {
            GameRealTime gameRealTime = World.Any<GameRealTime>();
            ActiveSceneLoadTime = gameRealTime?.PlayRealTime ?? new ARTimeSpan();
        }

#if UNITY_EDITOR && !SCENES_PROCESSED
        static void OnSceneLoaded(string sceneName) {
            var scene = SceneManager.GetSceneByName(sceneName);
            ProcessSceneContextIndependent(scene);
            var mapScene = GameObjects.FindComponentByTypeInScene<IMapScene>(scene, true);
            if (mapScene != null) {
                if (mapScene is SubdividedScene subdividedScene) {
                    subdividedScene.OnLoadedAllSubscenes += OnLoadedAllSubscenes;
                } else {
                    ProcessSceneContextDependent(scene);
                }
            } else {
                var subscene = GameObjects.FindComponentByTypeInScene<SubdividedSceneChild>(scene, true);
                if (!subscene) {
                    ProcessSceneContextDependent(scene);
                }
            }
        }

        static void OnLoadedAllSubscenes(SubdividedScene subdividedScene, List<SceneReference> subscenesReferences) {
            ProcessSceneContextDependent(subdividedScene.gameObject.scene);
            if (subdividedScene.mapStaticScene?.LoadedScene.IsValid() ?? false) {
                ProcessSceneContextDependent(subdividedScene.mapStaticScene.LoadedScene);
            }

            foreach (var subsceneRef in subscenesReferences) {
                if (subsceneRef.LoadedScene.IsValid() == false) {
                    Log.Important?.Error("Subscene loaded scene is not valid but at this point it should be valid");
                    continue;
                }

                ProcessSceneContextDependent(subsceneRef.LoadedScene);
            }
        }

        public static void ProcessSceneContextIndependent(Scene scene) {
            InvokeProcessSceneWithReflection(scene, true);
        }

        public static void ProcessSceneContextDependent(Scene scene) {
            InvokeProcessSceneWithReflection(scene, false);
        }

        static void InvokeProcessSceneWithReflection(Scene scene, bool useContextIndependentProcessors) {
            try {
                var scenesProcessingClassType = Type.GetType(
                    "Awaken.Utility.Editor.Scenes.ScenesProcessing, Awaken.Utility.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    true, true);
                var processSceneMethod = scenesProcessingClassType.GetMethod("ProcessScene", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                processSceneMethod!.Invoke(null, new object[] { scene, useContextIndependentProcessors });
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        static void ResetSceneProcessedStatusWithReflection(Scene scene) {
            try {
                var sceneProcessorClassType = Type.GetType(
                    "Awaken.Utility.Editor.Scenes.SceneProcessor, Awaken.Utility.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                    true, true);
                var clearCacheForSceneMethod = sceneProcessorClassType.GetMethod("ResetSceneProcessedStatus", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                clearCacheForSceneMethod!.Invoke(null, new object[] { scene });
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
#endif
    }
}