using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Editor.Assets.Grouping;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.Graphics;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Scenes.SubdividedScenes;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Editor.Utility.Building;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.UI.TitleScreen.FileVerification;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.Extensions;
using JetBrains.Annotations;
using Pathfinding;
using System.Diagnostics;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.Editor.DrakeRenderer;
using Awaken.ECS.Editor.MedusaRenderer;
using Awaken.ECS.MedusaRenderer;
using Awaken.Kandra.Managers;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Skills;
using Awaken.Utility.GameObjects;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Scenes;
using Unity.CodeEditor;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BuildCompression = UnityEngine.BuildCompression;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Path = System.IO.Path;
using PlayerSettings = UnityEditor.PlayerSettings;
using SceneReference = Awaken.TG.Assets.SceneReference;
#if UNITY_PS5
using UnityEditor.PS5;
#endif

namespace Awaken.TG.Editor.Utility {
    public static class BuildTools {
        public static string ContentBuilderPath => PlatformUtils.IsConsole
            ? "Builds"
            : "Steamworks/tools/ContentBuilder/content";

        public const string ExtraDefinesPrefix = "ExtraDefines:";
        public const string ArArgumentsPrefix = "ArArguments:";
        const string BuiltAddressablesPath = "Library/com.unity.addressables";

        static string[] s_arArguments;
        static string[] ArArguments => s_arArguments ??= ExtractArguments(ArArgumentsPrefix) ?? Array.Empty<string>();

        static string[] EditorBuildScenes => EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        public static readonly Dictionary<BuildTarget, BuildPathOption> BuildPaths = new() {
            { BuildTarget.StandaloneWindows64, new BuildPathOption("windows_content", "Fall of Avalon.exe") },
            { BuildTarget.StandaloneLinux64, new BuildPathOption("linux_content", "Fall_of_Avalon.x86_64") },
            { BuildTarget.StandaloneOSX, new BuildPathOption("mac_content", "Fall of Avalon") },
            { BuildTarget.GameCoreXboxSeries, new BuildPathOption("xbox_content", "") },
            { BuildTarget.PS5, new BuildPathOption("ps5_content", "Fall of Avalon") },
        };

        static BuildPathOption s_paths;
        static string[] s_overridenArguments;
        static string s_extraDefines;

        static readonly string
            XboxConfigsPath = Application.dataPath + "/../ProjectSettings",
            XboxConfig = XboxConfigsPath + "/ScarlettGame.config",
            XboxDemoConfig = XboxConfigsPath + "/ScarlettGame_Demo.config",
            XboxDevConfig1 = XboxConfigsPath + "/ScarlettGame_Dev_1.config",
            XboxDevConfig2 = XboxConfigsPath + "/ScarlettGame_Dev_2.config",
            XboxDevConfig3 = XboxConfigsPath + "/ScarlettGame_Dev_3.config",
            XboxDevConfig4 = XboxConfigsPath + "/ScarlettGame_Dev_4.config";

        static readonly string
            SteamConfigsPath = Application.dataPath + "/../Steamworks/tools/ContentBuilder/scripts/tainted_grail",
            SteamAppVdfPath = SteamConfigsPath + "/app_build_1831660.vdf",
            SteamDepotVdfPath = SteamConfigsPath + "/depot_build_1831661.vdf";

        static readonly string
            SteamDemoConfigsPath = Application.dataPath + "/../Steamworks/tools/ContentBuilder/scripts/tainted_grail_demo",
            SteamDemoAppVdf = SteamDemoConfigsPath + "/app_build_1831660.vdf",
            SteamDemoDepotVdf = SteamDemoConfigsPath + "/depot_build_1831661.vdf";

        // === Manual/Jenkins operations
        [UsedImplicitly]
        public static void BuildGeneral() {
            switch (EditorUserBuildSettings.selectedBuildTargetGroup) {
                case BuildTargetGroup.Standalone when HasArgument("linux"):
                    BuildProductionLinux();
                    break;
                case BuildTargetGroup.Standalone:
                    BuildProductionWindows(null, null);
                    break;
                case BuildTargetGroup.GameCoreXboxOne:
                    BuildXOne();
                    break;
                case BuildTargetGroup.GameCoreXboxSeries:
                    BuildScarlett();
                    break;
                case BuildTargetGroup.PS5:
                    BuildPS5Production();
                    break;
            }
        }

        [UsedImplicitly]
        static void ReimportAll() {
            EditorApplication.ExecuteMenuItem("Assets/Reimport All");
        }

        [MenuItem("TG/Build/Build Scripts Only", false, 0)]
        public static void BuildScriptsOnly() {
            CheckDefines();

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildTargetToGroup(buildTarget);
            s_paths = BuildPaths[buildTarget];

            BuildPlayerOptions options = new() {
                target = buildTarget,
                targetGroup = buildTargetGroup,
                subtarget = GetSubtarget(buildTargetGroup, true),
                locationPathName = s_paths.BuildPath,
                scenes = EditorBuildScenes,
                extraScriptingDefines = ExtractArguments(ExtraDefinesPrefix),
            };

            if (HasArgument("debug")) {
                EditorUserBuildSettings.development = true;
                options.options = BuildOptions.Development;
            }

            if (PlatformUtils.IsPS5) {
                PS5Builder.SetPS5BuildOptionsForScriptsOnly();
            }

            BuildReport buildReport = BuildPipeline.BuildPlayer(options);
            BuildResult result = buildReport.summary.result;

            Log.Important?.Info($"Building scripts result: {result}");
            if (Application.isBatchMode) {
                EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        [MenuItem("TG/Build/Windows", false, 0)]
        public static void BuildProductionWindowsManual() {
            OverridesWizard.ShowForOverrides(static (overrides, extraDefines) => {
                BuildProductionWindows(overrides, extraDefines);
                string fullDir = $"{Application.dataPath}/../{s_paths.BuildDirectory}";
                EditorUtility.RevealInFinder(fullDir);
            });
        }

        [MenuItem("TG/Build/Linux", false, 0)]
        public static void BuildProductionLinux() {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            BuildProduction(BuildTarget.StandaloneLinux64);
        }

        [MenuItem("TG/Build/Mac", false, 0)]
        public static void BuildProductionMac() {
            BuildProduction(BuildTarget.StandaloneOSX);
        }

        [MenuItem("TG/Build/Regenerate Spec Registry", false, 4000)]
        public static void RegenerateSpecRegistry() {
            foreach (string scenePath in GetAllScenes()) {
                using SceneResources sr = new(scenePath, false); //Opens the scene
                CacheSceneSpecsForOpenScenes();
            }
        }

        [MenuItem("TG/Build/Regenerate project files")]
        public static void RegenerateProjectFiles() {
            var currentEditor = CodeEditor.Editor.CurrentCodeEditor;
            if (currentEditor == null) {
                Log.Important?.Error("Couldn't find current code editor");
                return;
            }

            currentEditor.SyncAll();
        }

        [MenuItem("TG/Build/Refresh GUID cache")]
        public static void RefreshGUIDCache() {
            GUIDCache.Load();
            GUIDCache.Instance.Refresh(16);
        }

        [MenuItem("TG/Build/Xbox", false, 0)]
        public static void BuildXbox() {
            OverridesWizard.ShowForOverrides(static (overrides, extraDefines) => {
                OverrideArguments(overrides, extraDefines ?? string.Empty);
                MicrosoftGameVersionAccepted(InitXboxBuild);
            });
        }

        [MenuItem("TG/Build/PS5", false, 0)]
        static void BuildPS5() {
            OverridesWizard.ShowForOverrides(static (overrides, extraDefines) => {
                OverrideArguments(overrides, extraDefines ?? string.Empty);
                BuildPS5Production();
                string fullDir = $"{Application.dataPath}/../{s_paths.BuildDirectory}";
                EditorUtility.RevealInFinder(fullDir);
            });
        }

        static void BuildPS5Production() {
            PS5Builder.SetPS5BuildOptions();
            BuildProduction(BuildTarget.PS5);
        }

        // === Main build methods
        static void BuildProductionWindows(string[] overrides, string extraDefines) {
            try {
                OverrideArguments(overrides, extraDefines ?? string.Empty);
                BuildProduction(BuildTarget.StandaloneWindows64);
            } finally {
                s_overridenArguments = null;
                s_extraDefines = null;
            }
        }

        static void BuildProduction(BuildTarget buildTarget) {
            BuildTargetGroup buildTargetGroup = BuildTargetToGroup(buildTarget);
            if (EditorUserBuildSettings.activeBuildTarget != buildTarget) {
                throw new Exception($"Build target mismatch! Active build target is: {EditorUserBuildSettings.activeBuildTarget}. Requested build target is: {buildTarget}");
            }

            BuildPlayerOptions options = new() {
                target = buildTarget,
                targetGroup = buildTargetGroup,
                subtarget = GetSubtarget(buildTargetGroup),
            };
            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (HasArgument("debug")) {
                EditorUserBuildSettings.development = true;
                options.options = BuildOptions.Development;
                if (HasArgument("deep_profiling")) {
                    options.options |= BuildOptions.EnableDeepProfilingSupport;
                }

                if (HasArgument("connect_profiler")) {
                    options.options |= BuildOptions.ConnectWithProfiler;
                }

                if (HasArgument("script_debugging")) {
                    options.options |= BuildOptions.AllowDebugging;
                }

                if (HasArgument("wait_for_managed_debugger")) {
                    EditorUserBuildSettings.waitForManagedDebugger = true;
                }
            }

            if (HasArgument("clean_build")) {
                options.options |= BuildOptions.CleanBuildCache;
            }

            SetCopyPDB();
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            if (HasArgument("il2cpp") || PlatformUtils.IsConsole) {
                PlayerSettings.SetScriptingBackend(namedBuildTarget, ScriptingImplementation.IL2CPP);
                var compilerConfig = Il2CppCompilerConfiguration.Release;
                if (HasArgument("debug")) {
                    compilerConfig = Il2CppCompilerConfiguration.Debug;
                } else if (HasArgument("il2cpp_master")) {
                    compilerConfig = Il2CppCompilerConfiguration.Master;
                }

                PlayerSettings.SetIl2CppCompilerConfiguration(namedBuildTarget, compilerConfig);
                PlayerSettings.SetIl2CppCodeGeneration(namedBuildTarget, Il2CppCodeGeneration.OptimizeSpeed);
            } else {
                PlayerSettings.SetScriptingBackend(namedBuildTarget, ScriptingImplementation.Mono2x);
            }

            if (PlatformUtils.IsConsole) {
                EditorUserBuildSettings.explicitNullChecks = true;
                EditorUserBuildSettings.explicitArrayBoundsChecks = true;
            }

            CheckGamePass();
            if (PlatformUtils.IsXbox) {
                XboxBuilder.SetGameCoreBuildSettings();
            }

            bool buildResult = PerformBuild(options, out _);

            if (Application.isBatchMode) {
                EditorApplication.Exit(buildResult ? 0 : 1);
            } else if (HasArgument("shutdown_after")) {
                var processInfo = new ProcessStartInfo("shutdown", "/s /f /t 0");
                Process.Start(processInfo);
                EditorApplication.Exit(0);
            }
        }
        
        public static bool PerformBuild(BuildPlayerOptions options, out string buildDirectory) {
            SceneProcessor.ResetAllScenesProcessedStatus();
            // Set Addressables to Use Asset Database
            AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = 0;

            s_paths = BuildPaths[options.target];
            buildDirectory = s_paths.BuildDirectory;

            if (!Application.isBatchMode) {
                // Refresh GUID Cache
                bool stripAddressables = HasArgument("strip_unused_addressables");
                if (HasArgument("refresh_guid_cache") || stripAddressables) {
                    RefreshGUIDCache();
                }

                //Bake Loot Cache
                if (HasArgument("bake_scene_cache")) {
                    BakeSceneCache();
                }
                
                // Unload views
                UnloadViews.UnloadAllView();
                
                // Process scenes
                bool buildAddressables = HasArgument("build_addressables");

                if (buildAddressables) {
                    if (HasArgument("process_hos_only")) {
                        PrepareHosOnlyScenes();
                    }
                    
                    if (stripAddressables) {
                        AddressablesCleaner.Cleaner.PerformBuildCleaning();
                    }

                    if (HasArgument("process_scenes_and_assets")) {
                        PrepareDefines(options.target);
                        ProcessScenes(GetAllScenes());
                    }
                    
                    PrepareAndBuildAddressables();
                }
            
                if (!HasArgument("actually_build")) {
                    return false;
                }
            }

            if (!VerifyAddressablesBuild()) {
                return false;
            }

            // Set options
            options.locationPathName = s_paths.BuildPath;
            options.scenes = EditorBuildScenes;

            string[] extraDefines = ExtractArguments(ExtraDefinesPrefix);
            options.extraScriptingDefines = extraDefines;
            if (options.extraScriptingDefines != null) {
                Log.Important?.Info($"<color=blue>Extra defines:</color> <color=yellow>{string.Join(", ", options.extraScriptingDefines)}</color>");
            }

            // Build
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(options.targetGroup));
            Log.Important?.Info($"<color=blue>Starting build task with flags</color> <color=yellow>{defines}</color>");
            var report = BuildPipeline.BuildPlayer(options);

            if (File.Exists("Temp/shader-stripping.json")) {
                File.Copy("Temp/shader-stripping.json", Path.Combine(s_paths.BuildDirectory, "shader-stripping.json"), true);
                File.Copy("Temp/shader-stripping.json", "shader-stripping.json", true);
            }

            var summary = report.summary;
            BuildResult summaryResult = summary.result;
#if UNITY_PS5
            var packageResult = PS5Builder.CreatePackage(s_paths.BuildPath, report, HasArgument("submission"));
            if (!packageResult) {
                summaryResult = BuildResult.Failed;
            }
#endif
            Log.Important?.Info($"Build summary: Result: {ColorResult(summaryResult)},\n" +
                                $"\tStarted at: {summary.buildStartedAt},\n" +
                                $"\tEnded at: {summary.buildEndedAt},\n" +
                                $"\tTime: {summary.totalTime},\n" +
                                $"\tPath: {summary.outputPath},\n" +
                                $"\tTarget: {summary.platform},\n" +
                                $"\tTarget group: {summary.platformGroup},\n" +
                                $"\tSize: {summary.totalSize},\n" +
                                $"\tErrors: {summary.totalErrors},\n" +
                                $"\tWarnings: {summary.totalWarnings},\n",
                logOption: LogOption.NoStacktrace);

            if (PlatformUtils.IsWindows) {
                FileChecksum.Create(s_paths.BuildDirectory);
            }

            return summaryResult == BuildResult.Succeeded;

            static string ColorResult(BuildResult result) {
                string color = result switch {
                    BuildResult.Succeeded => "green",
                    BuildResult.Failed => "red",
                    _ => "gray"
                };
                return $"<color={color}>{result}</color>";
            }
        }

        public static void ProcessScenes(string[] scenesToProcessPaths) {
            ClearLibraryAssetsArtifacts();

            using var buildBaking = new BuildSceneBaking();

            // Using allScenesPaths as a parameter to allow processing subset of all scenes if it is needed to 
            // test scenes processing quickly 
            foreach (string scenePath in scenesToProcessPaths) {
                // Don't process subscenes as they are processed in the main scene
                if (IsSubsceneByPath(scenePath)) {
                    continue;
                }

                try {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Log.Debug?.Info($"Process scene {scenePath}");
                    
                    CacheSceneSpecsForScene(scene);
                    PrepareGroundForBuild(scene);
                    ProcessScenePreProcessors(scene);
                    ProcessStaticDecals(scene);
                    ScenesStaticSubdivision.ExecuteStaticSubdivisionForBuild(scene, out var subdividedScene, out var staticScene, true, false);
                    UnpackAllPrefabsInScene(scene);
                    if (staticScene.IsValid()) {
                        UnpackAllPrefabsInScene(staticScene);
                    }
                    AssetDatabase.SaveAssets();
                    ScenesProcessing.ProcessScene(scene, useContextIndependentProcessors: true);
                    if (staticScene.IsValid()) {
                        ScenesProcessing.ProcessScene(staticScene, useContextIndependentProcessors: true);
                    }

                    if (subdividedScene) {
                        UnityEngine.Pool.ListPool<Scene>.Get(out var subscenes);

                        new SubdividedScene.EditorAccess(subdividedScene).LoadAllScenes(true, subscenes);

                        var mergedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                        var roots = new List<GameObject>();
                        foreach (var subscene in subscenes) {
                            CacheSceneSpecsForScene(subscene);
                            ProcessScenePreProcessors(subscene);

                            roots.EnsureCapacity(subscene.rootCount);
                            subscene.GetRootGameObjects(roots);
                            var rootIds = new NativeArray<int>(roots.Count, ARAlloc.TempJob); // TempJob to correct dispose when building
                            for (int i = 0; i < roots.Count; i++) {
                                RemoveInvalidObjectBeforeMerge(roots[i]);

                                rootIds[i] = roots[i].GetInstanceID();
                            }

                            SceneManager.MoveGameObjectsToScene(rootIds, mergedScene);
                            rootIds.Dispose();
                            roots.Clear();
                        }

                        roots.EnsureCapacity(mergedScene.rootCount);
                        mergedScene.GetRootGameObjects(roots);
                        RemoveInvalidComponentsAfterMerge(roots);

                        string directory = Path.GetDirectoryName(subdividedScene.gameObject.scene.path);
                        string newScenePath = Path.Combine(directory, $"{subdividedScene.gameObject.scene.name}_merged.unity");
                        EditorSceneManager.SaveScene(mergedScene, newScenePath);

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        var staticSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(newScenePath);
                        AddressableHelper.AddEntry(new AddressableEntryDraft.Builder(staticSceneAsset)
                            .InGroup(AddressableGroup.Scenes).WithLabels(SceneService.ScenesLabel)
                            .WithAddressProvider(static (obj, _) => obj.name).Build());
                        var mergedSceneRef = SceneReference.ByAddressable(new ARAssetReference(AssetDatabase.AssetPathToGUID(newScenePath)));

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        new SubdividedScene.EditorAccess(subdividedScene).ReplaceSubscenesWithMerged(mergedSceneRef);

                        ScenesStaticSubdivision.ExecuteStaticSubdivisionForBuild(mergedScene, out _, out var mergedStaticScene, false, false);
                        UnpackAllPrefabsInScene(mergedScene);
                        UnpackAllPrefabsInScene(mergedStaticScene);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        ScenesProcessing.ProcessScene(mergedScene, useContextIndependentProcessors: true);
                        ScenesProcessing.ProcessScene(mergedStaticScene, useContextIndependentProcessors: true);

                        ScenesStaticSubdivision.UpdateStaticScenesConfigs();
                        subdividedScene.RefreshStaticScenesList();

                        EditorUtility.SetDirty(subdividedScene);
                        EditorSceneManager.MarkAllScenesDirty();
                        EditorSceneManager.SaveScene(mergedScene);
                        EditorSceneManager.SaveScene(mergedStaticScene);
                        EditorSceneManager.SaveScene(staticScene);
                        EditorSceneManager.SaveScene(subdividedScene.gameObject.scene);
                        EditorSceneManager.MarkAllScenesDirty();
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        ScenesProcessing.ProcessScene(scene, useContextIndependentProcessors: false);
                        ScenesProcessing.ProcessScene(staticScene, useContextIndependentProcessors: false);

                        ScenesProcessing.ProcessScene(mergedScene, useContextIndependentProcessors: false);
                        ScenesProcessing.ProcessScene(mergedStaticScene, useContextIndependentProcessors: false);

                        foreach (var subscene in subscenes) {
                            var subscenePath = subscene.path;
                            var guid = AssetDatabase.AssetPathToGUID(subscenePath);
                            AddressableHelper.RemoveEntry(guid);
                        }

                        UnityEngine.Pool.ListPool<Scene>.Release(subscenes);
                    } else {
                        ScenesStaticSubdivision.UpdateStaticScenesConfigs();
                        AssetDatabase.SaveAssets();

                        ScenesProcessing.ProcessScene(scene, useContextIndependentProcessors: false);
                        if (staticScene.IsValid()) {
                            ScenesProcessing.ProcessScene(staticScene, useContextIndependentProcessors: false);
                        }
                    }
                    AssetDatabase.SaveAssets();
                    MarkAllObjectsInOpenScenesDirty();
                    EditorSceneManager.SaveOpenScenes();
                } catch (Exception e) {
                    Log.Critical?.Error($"Exception while processing scene {scenePath}");
                    Debug.LogException(e);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode) {
                AddScenesProcessedDefine();
            }

            static void RemoveInvalidObjectBeforeMerge(GameObject current) {
                if (current.hideFlags.HasCommonBitsFast(HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor)) {
                    Object.DestroyImmediate(current);
                    return;
                }

                var sceneVariables = current.GetComponentsInChildren<SceneVariables>();
                for (int k = 0; k < sceneVariables.Length; k++) {
                    Object.DestroyImmediate(sceneVariables[k]);
                }

                var rootTransform = current.transform;
                var childrenCount = current.transform.childCount;
                for (int j = childrenCount - 1; j >= 0; j--) {
                    var child = rootTransform.GetChild(j);
                    RemoveInvalidObjectBeforeMerge(child.gameObject);
                }
            }

            static void RemoveInvalidComponentsAfterMerge(List<GameObject> roots) {
                Type[] uniqueTypes = new Type[] {
                    typeof(SubdividedSceneChild),
                    typeof(DistanceCuller),
                };

                var presentBitmask = new UnsafeBitmask((uint)uniqueTypes.Length, ARAlloc.TempJob);

                foreach (var root in roots) {
                    for (var typeIndex = 0u; typeIndex < uniqueTypes.Length; typeIndex++) {
                        Type uniqueType = uniqueTypes[typeIndex];
                        var components = root.GetComponentsInChildren(uniqueType, true);
                        for (var i = 0u; i < components.Length; i++) {
                            if (presentBitmask[typeIndex]) {
                                Object.DestroyImmediate(components[i]);
                            }

                            presentBitmask.Up(typeIndex);
                        }
                    }
                }

                presentBitmask.Dispose();
            }
        }
        
        public static void PrepareAndBuildAddressables() {
            if (Application.isBatchMode || HasArgument("process_scenes_and_assets")) {
                UnpackKandra();
                BakeStory();
                SkillGraphBuildTools.PrepareForBuild();
                LocalizationTools.PrepareForBuild();
                PrepareArchives();
                AssetDatabase.Refresh();
            }
            BuildAddressables();
        }

        // === Build Steps
        [MenuItem("TG/Build/Baking/Unpack Kandra", false, -3000)]
        static void UnpackKandra() {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab")) {
                    try {
                        UnpackKandra(AssetDatabase.GUIDToAssetPath(guid));
                    } catch (Exception e) {
                        Log.Important?.Error("Failed to unpack kandra prefab. See log below.");
                        Debug.LogException(e);
                    }
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }

        [MenuItem("TG/Build/Baking/Bake Story", false, -3000)]
        static void BakeStory() {
            if (Directory.Exists(StoryGraphRuntime.BakingDirectoryPath)) {
                Directory.Delete(StoryGraphRuntime.BakingDirectoryPath, true);
            }
            Directory.CreateDirectory(StoryGraphRuntime.BakingDirectoryPath);

            var group = AddressableHelper.FindGroup("Templates.Story");
            foreach (var entry in group.entries.ToArray()) {
                if (entry.MainAsset is StoryGraph graph) {
                    graph.GUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graph));
                    try {
                        StoryGraphParser.Serialize(graph);
                    } catch (Exception e) {
                        Log.Critical?.Error($"Exception below while baking Story {graph.name}({graph.GUID})");
                        Debug.LogException(e);
                    }
                    group.RemoveAssetEntry(entry, false);
                }
            }
            group.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        }

        [MenuItem("TG/Build/Baking/Prepare archives", false, -3001)]
        static void PrepareArchives() {
            // MakeArchiveFromLibrary(HLODLoadManager.SubdirectoryName, HLODLoadManager.ArchiveFileName);
            MakeArchiveFromLibrary(DrakeMergedRenderersLoading.SubdirectoryName, DrakeMergedRenderersLoading.ArchiveFileName);
            MakeArchiveFromLibrary(MedusaPersistence.SubdirectoryName, MedusaPersistence.ArchiveFileName);
            MakeArchiveFromLibrary(StreamedSkillGraphs.SubdirectoryName, StreamedSkillGraphs.ArchiveFileName);
            MakeArchiveFromLibrary(StoryGraphRuntime.SubdirectoryName, StoryGraphRuntime.ArchiveFileName);
            MakeArchiveFromStreamingAssets(StreamingManager.SubdirectoryName, StreamingManager.ArchiveFileName);

            if (!Application.isBatchMode) {
                AddArchivesProducedDefine();
            }

            void MakeArchiveFromLibrary(string subdirectoryName, string archiveFileName) {
                MakeArchive("Library", subdirectoryName, archiveFileName, false);
            }

            void MakeArchiveFromStreamingAssets(string subdirectoryName, string archiveFileName) {
                MakeArchive(Application.streamingAssetsPath, subdirectoryName, archiveFileName, true);
            }

            void MakeArchive(string startingPath, string subdirectoryName, string archiveFileName, bool removeInput) {
                var bakingDirectoryPath = Path.Combine(startingPath, subdirectoryName);
                if (!Directory.Exists(bakingDirectoryPath)) {
                    return;
                }
                var inputPaths = Directory.EnumerateFiles(bakingDirectoryPath, "*", SearchOption.AllDirectories).ToArray();
                var inputFiles = new ResourceFile[inputPaths.Length];
                for (var i = 0; i < inputPaths.Length; i++) {
                    var relativePath = Path.GetRelativePath(bakingDirectoryPath, inputPaths[i]).Replace('\\', '/');
                    inputFiles[i] = new ResourceFile() {
                        fileName = inputPaths[i],
                        fileAlias = relativePath,
                    };
                }

                var archivePathDirectory = Path.Combine(Application.streamingAssetsPath, subdirectoryName);
                if (!Directory.Exists(archivePathDirectory)) {
                    Directory.CreateDirectory(archivePathDirectory);
                }
                var archivePath = Path.Combine(archivePathDirectory, archiveFileName);
                if (File.Exists(archivePath)) {
                    File.Delete(archivePath);
                }

                ContentBuildInterface.ArchiveAndCompress(inputFiles, archivePath, BuildCompression.Uncompressed, true);

                if (removeInput) {
                    for (var i = 0; i < inputPaths.Length; i++) {
                        File.Delete(inputPaths[i]);
                    }
                }
            }
        }

        [MenuItem("TG/Build/Baking/Unpack Kandra Selected", false, -3000)]
        static void UnpackKandraSelected() {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (var go in Selection.gameObjects) {
                    try {
                        UnpackKandra(AssetDatabase.GetAssetPath(go));
                    } catch (Exception e) {
                        Log.Important?.Error("Failed to unpack kandra prefab. See log below.");
                        Debug.LogException(e);
                    }
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }

        static List<Animator> UnpackKandra_AnimatorsBuffer = new List<Animator>(8);
        static void UnpackKandra(string path) {
            if (path.IsNullOrWhitespace()) {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var rigs = asset.GetComponentsInChildren<KandraRig>();
            if (rigs.Length == 0) {
                return;
            }

            using var prefabScope = new PrefabUtility.EditPrefabContentsScope(path);
            var prefab = prefabScope.prefabContentsRoot.transform;
            UnpackAnyPrefab(prefab);

            prefab.GetComponentsInChildren(UnpackKandra_AnimatorsBuffer);
            foreach (var animator in UnpackKandra_AnimatorsBuffer) {
                var avatar = animator.avatar;
                var avatarPath = AssetDatabase.GetAssetPath(avatar);
                if (avatarPath.EndsWith(".fbx")) {
                    var newAvatarPath = avatarPath[..^4] + "_Avatar.asset";
                    var newAvatar = Object.Instantiate(avatar);
                    AssetDatabase.CreateAsset(newAvatar, newAvatarPath);
                    animator.avatar = newAvatar;
                }
            }
            UnpackKandra_AnimatorsBuffer.Clear();
        }

        static void BuildAddressables() {
            var addressableManager = AssetDatabase.LoadAssetAtPath<ARAddressableManager>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:ARAddressableManager").First()));
            
            if (Application.isBatchMode || HasArgument("update_ar_addressables")) {
                addressableManager.UpdateData();
                if (!addressableManager.assignGroups) {
                    addressableManager.AssignEntriesToAddressables();
                }
            } else {
                addressableManager.AssignEntriesToAddressables();
            }

            ProjectConfigData.GenerateBuildLayout = HasArgument("addressables_build_layout");

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (result.Error.IsNullOrWhitespace()) {
                Log.Important?.Info("Addressables build succeeded with no errors.");
            } else {
                throw new Exception($"Addressables build failed: {result.Error}");
            }
        }

        [MenuItem("TG/Build/Baking/Bake Scene Cache", false, 100)]
        static void BakeSceneCache() {
            TemplatesSearcher.EnsureInit();

            try {
                SceneCacheBaker.Bake();
            } catch {
                Debug.LogError("Failed to bake scene cache");
                throw;
            }
        }

        [MenuItem("TG/Build/Clear library artifacts", false, 110)]
        public static void ClearLibraryAssetsArtifacts() {
            // BuildProcess.ClearHLODsLibraryAssets();
            DrakeSceneBaker.ClearDrakeLibraryAssets();
            MedusaRendererManagerBaker.ClearMedusaLibraryAssets();
        }

        // === Per scene actions
        [MenuItem("TG/Build/Baking/Cache Specs", false, -3000)]
        static void CacheSceneSpecsForAllScenes() {
            var initiallyLoadedScenesLoader = new InitiallyLoadedScenesLoader();
            initiallyLoadedScenesLoader.SaveCurrentScenesAsInitiallyLoaded();
            var allScenes = GetAllScenes();
            using var bake = new BuildSceneBaking();
            foreach (var scenePath in allScenes) {
                if (EditorScenesUtility.TryOpenSceneSingle(scenePath, out var scene) == false) {
                    continue;
                }

                CacheSceneSpecsForScene(scene);
                EditorSceneManager.SaveScene(scene);
            }

            initiallyLoadedScenesLoader.RestoreInitiallyLoadedScenes();
        }

        [MenuItem("TG/Build/Baking/Cache Specs for current scenes", false, -3000)]
        static void CacheSceneSpecsForOpenScenes() {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                CacheSceneSpecsForScene(SceneManager.GetSceneAt(i));
            }
        }
        
        static void CacheSceneSpecsForScene(Scene scene) {
            var sceneSpecs = GameObjects.FindComponentsByTypeInScene<SceneSpec>(scene, false, 64);
            foreach (var sceneSpec in sceneSpecs) {
                try {
                    sceneSpec.CacheSceneId();
                    EditorUtility.SetDirty(sceneSpec);
                } catch (Exception e) {
                    Log.Critical?.Error($"Cannot cache scene id for {sceneSpec}. Exception below", sceneSpec);
                    Debug.LogException(e);
                }
            }
        }

        [MenuItem("TG/Build/Baking/Prepare ground current scenes", false, -3000)]
        static void PrepareGroundForBuildInOpenScenes() {
            var sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++) {
                PrepareGroundForBuild(SceneManager.GetSceneAt(i));
            }
        }
        
        static void PrepareGroundForBuild(Scene scene) {
            if (TryRefreshGroundBounds(scene, out var groundBounds)) {
                BakeMedusaVisibility(scene, groundBounds);
                BakeTerrainVisibility(scene, groundBounds);
            }
            
            CheckIfTerrainsExist(scene);
        }

        [MenuItem("TG/Build/Baking/Prepare current scenes", false, -3000)]
        static void PrepareCurrentScenes() {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                PrepareGroundForBuild(scene);
            }
            
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            ScenesStaticSubdivision.SimulateStaticSubdivisionForCurrentScenes();
        }
        
        static void ProcessScenePreProcessors(Scene scene) {
            var preProcessors = GameObjects.FindComponentsByTypeInScene<ScenePreProcessorComponent>(scene, false);
            foreach (var preProcessor in preProcessors) {
                preProcessor.Process();
                Object.DestroyImmediate(preProcessor);
            }
        }

        static void ProcessStaticDecals(Scene scene) {
            var mapScene = GameObjects.FindComponentByTypeInScene<IMapScene>(scene, false);
            if (mapScene == null) {
                return;
            }
            var component = (Component)mapScene;
            var enabledDecalsCuller = mapScene is not SubdividedScene;

            var staticDecals = GameObjects.FindComponentsByTypeInScene<StaticDecalsCuller>(scene, !enabledDecalsCuller);
            if (staticDecals.Count == 0) {
                var decalsCuller = component.gameObject.AddComponent<StaticDecalsCuller>();
                decalsCuller.enabled = false;
            }
        }

        static bool TryRefreshGroundBounds(Scene scene, out GroundBounds groundBounds) {
            groundBounds = GameObjects.FindComponentByTypeInScene<GroundBounds>(scene, false);
            if (groundBounds != null) {
                new GroundBounds.EditorAccess(groundBounds).SetupRelatedSystems();
                return true;
            }

            return false;
        }

        static void BakeMedusaVisibility(Scene scene, GroundBounds groundBounds) {
            if (!groundBounds) {
                return;
            }

            var medusaGroundBakers = GameObjects.FindComponentsByTypeInScene<MedusaGroundBoundsBaker>(scene, true, 24);
            foreach (var baker in medusaGroundBakers) {
                baker.Bake(groundBounds);
            }
        }

        [MenuItem("TG/Build/Baking/Bake terrain visible", false, -3000)]
        static void BakeTerrainVisibility() {
            var groundBounds = Object.FindAnyObjectByType<GroundBounds>();
            if (!groundBounds) {
                return;
            }

            var terrainGroundBaker = Object.FindAnyObjectByType<TerrainGroundBoundsBaker>();
            if (terrainGroundBaker) {
                terrainGroundBaker.Bake(groundBounds);
            }
        }

        static void BakeTerrainVisibility(Scene scene, GroundBounds groundBounds) {
            var terrainGroundBaker = GameObjects.FindComponentByTypeInScene<TerrainGroundBoundsBaker>(scene, false);
            if (terrainGroundBaker) {
                terrainGroundBaker.Bake(groundBounds);
            }
        }

        static void CheckIfTerrainsExistInOpenScenes() {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain != null) {
                throw new Exception($"Failed! Terrains shouldn't exist at this stage! Scene: {terrain.gameObject.scene.name}");
            }
        }
        
        static void CheckIfTerrainsExist(Scene scene) {
            var terrain = GameObjects.FindComponentByTypeInScene<Terrain>(scene, false);
            if (terrain != null) {
                throw new Exception($"Failed! Terrains shouldn't exist at this stage! Scene: {terrain.gameObject.scene.name}");
            }
        }

        [MenuItem("TG/Build/Unpack all prefabs in open scenes", false, -3000)]
        public static void UnpackAllPrefabsInOpenScenes() {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                UnpackAllPrefabsInScene(SceneManager.GetSceneAt(i));
            }
        }
        
        static void UnpackAllPrefabsInScene(Scene scene) {
            var rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++) {
                UnpackPrefabs(rootGameObjects[i]);
            }

            static void UnpackPrefabs(GameObject rootGameObject) {
                if (PrefabUtility.IsPartOfPrefabInstance(rootGameObject)) {
                    PrefabUtility.UnpackPrefabInstance(rootGameObject, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                    EditorUtility.SetDirty(rootGameObject);
                }
                for (int i = 0; i < rootGameObject.transform.childCount; i++) {
                    UnpackPrefabs(rootGameObject.transform.GetChild(i).gameObject);
                }
            }
        }

        [MenuItem("TG/Build/Mark all objects in open scenes dirty", false, -3000)]
        public static void MarkAllObjectsInOpenScenesDirty() {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                MarkAllObjectsInSceneDirty(SceneManager.GetSceneAt(i));
            }
        }
        
        static void MarkAllObjectsInSceneDirty(Scene scene) {
            var rootGameObjects = scene.GetRootGameObjects();
            UnityEngine.Pool.ListPool<Component>.Get(out var tempComponents);
            for (int i = 0; i < rootGameObjects.Length; i++) {
                MarkAllObjectsDirty(scene, rootGameObjects[i].transform, tempComponents);
            }

            UnityEngine.Pool.ListPool<Component>.Release(tempComponents);
            
            static void MarkAllObjectsDirty(Scene scene, Transform subject, List<Component> tempComponents) {
                for (int i = 0; i < subject.childCount; i++) {
                    MarkAllObjectsDirty(scene, subject.GetChild(i), tempComponents);
                }
                tempComponents.Clear();
                subject.gameObject.GetComponents(tempComponents);
                for (int i = 0; i < tempComponents.Count; i++) {
                    if (tempComponents[i] != null) {
                        EditorUtility.SetDirty(tempComponents[i]);
                    } else {
                        Log.Critical?.Error($"There is a null component in {subject.gameObject.name} in scene {scene.name}");
                    }
                }
                EditorUtility.SetDirty(subject.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }
        
        // === Player preparation
        static void CheckGamePass() {
            if (HasArgument("gamepass")) {
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
                Array.Resize(ref defines, defines.Length + 1);
                defines[^1] = "MICROSOFT_GAME_CORE";
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defines);
                SetCopyPDB();
            }
        }

        static void SetCopyPDB() {
            EditorUserBuildSettings.SetPlatformSettings("Standalone", "CopyPDBFiles", "true");
        }

        // === Xbox
        static void MicrosoftGameVersionAccepted(Action<string> onVersionChosen) {
            const string UnspecifiedVersion = "0.0.0.0";
            string gameConfigPath = XboxBuilder.GetGameConfigPath();
            if (string.IsNullOrEmpty(gameConfigPath)) {
                onVersionChosen.Invoke(UnspecifiedVersion);
                return;
            }

            System.Xml.Linq.XDocument gameConfigXmlDoc = System.Xml.Linq.XDocument.Load(gameConfigPath);
            System.Xml.Linq.XElement identityElement = (from identity in gameConfigXmlDoc.Descendants("Identity") select identity).First();
            System.Xml.Linq.XAttribute versionAttribute = identityElement.Attribute("Version");
            if (versionAttribute == null) {
                onVersionChosen.Invoke(UnspecifiedVersion);
                return;
            }

            var microsoftGameVersion = versionAttribute.Value;
            int result = EditorUtility.DisplayDialogComplex("Current Version in MS.Config",
                $"Current Version set in MicrosoftGame.Config is: {microsoftGameVersion}, do you want to proceed?",
                "Proceed", "Cancel", "Change Version");
            if (result == 0) {
                onVersionChosen.Invoke(versionAttribute.Value);
            } else if (result == 2) {
                XboxBuildVersionPopup.Show(onVersionChosen, gameConfigXmlDoc, gameConfigPath, versionAttribute);
            }
        }

        static void InitXboxBuild(string microsoftGameVersion) {
            int result = EditorUtility.DisplayDialogComplex(
                $"Platform target. MicrosoftGame Version: {microsoftGameVersion}",
                "Choose target console",
                "Xbox Series",
                "Cancel",
                "Xbox One");

            if (result == 0) {
                BuildScarlett();
            } else if (result == 2) {
                BuildXOne();
            }
        }

        static void BuildScarlett() {
            BuildProduction(BuildTarget.GameCoreXboxSeries);
        }

        static void BuildXOne() {
            BuildProduction(BuildTarget.GameCoreXboxOne);
        }

        // === Individual baking methods
        [MenuItem("TG/Build/Baking/Bake Occlusion Culling", false, 0)]
        public static void BakeOcclusionCulling() {
            ClearOcclusionCulling();
        }

        [MenuItem("TG/Build/Baking/Clear Occlusion Culling", false, 0)]
        public static void ClearOcclusionCulling() {
            var scenes = GetAllScenes();
            foreach (var scenePath in scenes) {
                using SceneResources sr = new(scenePath, false);
                StaticOcclusionCulling.Clear();
            }
        }

        [MenuItem("TG/Build/Baking/Bake NavMesh", false, 0)]
        public static void BakeNavMesh() {
            BakeNavMeshInScenes(GetAllScenes());
        }

        [MenuItem("TG/Build/Baking/Bake NavMesh in open scenes", false, 0)]
        public static void BakeNavMeshInOpenScenes() {
            BakeNavMeshInScenes(EditorScenesUtility.GetCurrentlyOpenScenesPath());
        }

        [MenuItem("TG/Build/Baking/Bake Lighting", false, 0)]
        public static void BakeLighting() {
            SceneConfigs sceneConfigs = GetSceneConfigs();
            if (sceneConfigs == null) {
                return;
            }

            var scenePaths = GetAllScenes();
            sceneConfigs.UpdateSceneList();
            foreach (string sceneName in sceneConfigs.ScenesToBake) {
                var scenePath = scenePaths.FirstOrDefault(sp => sp.EndsWith($"{sceneName}.unity"));
                if (string.IsNullOrEmpty(scenePath)) {
                    Log.Important?.Error($"Scene path for {sceneName} scene cannot be null or empty!");
                    continue;
                }

                using SceneResources sr = new(scenePath, false);
                BakeLightingForCurrentScene(sr.loadedScene, GetSceneConfigs());
            }
        }

        static void BakeNavMeshInScenes(string[] scenesPaths) {
            foreach (string scenePath in scenesPaths) {
                if (!IsSubsceneByPath(scenePath)) {
                    using SceneResources sr = new(scenePath, false);
                    BakeNavMesh(sr.loadedScene);
                }
            }
        }

        static void BakeNavMesh(Scene scene) {
            // Don't bake NavMesh for child scenes, NavMesh should be baked on mother scene
            if (IsChildScene()) return;
            var serializationSettings = Pathfinding.Serialization.SerializeSettings.Settings;
            serializationSettings.nodes = true;
            Directory.CreateDirectory(AstarData.PathfindingCacheDirectoryPath);
            TryRefreshGroundBounds(scene, out _);
            using (new NavMeshBakingPreparation(scene)) {
                AstarPathEditor.MenuScan();
                var astar = AstarPath.active;
                if (astar != null) {
                    byte[] bytes = astar.data.SerializeGraphs(serializationSettings);
                    var cacheFileName = astar.data.cacheFileName;
                    string oldPath = $"Assets/Data/PathfindingCache/{cacheFileName}";
                    if (File.Exists(oldPath)) {
                        File.Delete(oldPath);
                        File.Delete(oldPath + ".meta");
                    }

                    string path = astar.data.cacheFilePath;
                    Pathfinding.Serialization.AstarSerializer.SaveToFile(path, bytes);
                }
            }
        }

        static void BakeLightingForCurrentScene(Scene scene, SceneConfigs configs) {
            if (configs == null || !configs.ShouldBake()) return;

            using (new RenderersLoadedForBaking(scene)) {
                Lightmapping.Bake();
                //Ensure baking is completed before continuing
                while (Lightmapping.isRunning) { }
            }
        }

        // === Helpers
        static BuildTargetGroup BuildTargetToGroup(BuildTarget target) {
            return target switch {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneLinux64
                    or BuildTarget.StandaloneOSX => BuildTargetGroup.Standalone,
                BuildTarget.WebGL => BuildTargetGroup.WebGL,
                BuildTarget.WSAPlayer => BuildTargetGroup.WSA,
                BuildTarget.PS4 => BuildTargetGroup.PS4,
                BuildTarget.XboxOne => BuildTargetGroup.XboxOne,
                BuildTarget.tvOS => BuildTargetGroup.tvOS,
                BuildTarget.Switch => BuildTargetGroup.Switch,
                BuildTarget.LinuxHeadlessSimulation => BuildTargetGroup.LinuxHeadlessSimulation,
                BuildTarget.PS5 => BuildTargetGroup.PS5,
                BuildTarget.GameCoreXboxSeries => BuildTargetGroup.GameCoreXboxSeries,
                _ => BuildTargetGroup.Unknown
            };
        }

        static int GetSubtarget(BuildTargetGroup targetGroup, bool scriptsOnly = false) {
            return targetGroup switch {
                BuildTargetGroup.Standalone => (int)StandaloneBuildSubtarget.Player,
                BuildTargetGroup.GameCoreXboxSeries => (int)XboxBuilder.GetGameCoreSubtarget(),
                BuildTargetGroup.PS5 => PS5Builder.GetSubtarget(scriptsOnly),
                _ => 0,
            };
        }

        /// <summary>
        /// Do the build arguments contain the flag
        /// </summary>
        /// <param name="arg">the flag to find</param>
        /// <returns>Whether the flag was found</returns>
        public static bool HasArgument(string arg) {
            return HasArgument(arg, s_overridenArguments);
        }

        public static bool HasArgument(string arg, string[] overridenArguments) {
            bool hasOverriden = overridenArguments?.Any(oa => oa.Equals(arg, StringComparison.InvariantCultureIgnoreCase)) ?? false;
            bool hasEnvironment = ArArguments.Any(a => a.Equals(arg, StringComparison.InvariantCultureIgnoreCase));
            return hasOverriden || hasEnvironment;
        }

        static bool VerifyAddressablesBuild() {
            if (!Directory.Exists(BuildTools.BuiltAddressablesPath) ||
                !Directory.EnumerateFileSystemEntries(BuildTools.BuiltAddressablesPath).Any()) {
                Log.Important?.Error("Addressables failed to build properly. Check logs above for more information.");
                return false;
            }

            return true;
        }

        static string[] ExtractArguments(string argsPrefix) {
            // We need to set defines as null, because empty array is broken
            var arguments = Environment.GetCommandLineArgs()
                .FirstOrFallback(a => a.StartsWith(argsPrefix), s_extraDefines)?.Substring(argsPrefix.Length).Split(',') ?? Array.Empty<string>();
            arguments = arguments.Where(d => !string.IsNullOrWhiteSpace(d)).ToArray();
            arguments = arguments.Length > 0 ? arguments : null;
            return arguments;
        }

        public static string[] GetAllScenes() {
            var sceneEntries = AddressableHelper.FindGroup(SceneService.ScenesGroup).entries;
            var sceneEditorEntries = AddressableHelper.FindGroup(SceneService.ScenesEditorGroup).entries;
            return sceneEntries.Concat(sceneEditorEntries).Select(e => e.AssetPath).ToArray();
        }

        static void OverrideArguments(string[] args, string extraDefines) {
            s_overridenArguments = args;
            s_extraDefines = extraDefines;
        }

        public static SceneConfigs GetSceneConfigs() {
            return AssetDatabase.LoadAssetAtPath<SceneConfigs>(SceneConfigsWindow.SceneConfigAssetPath);
        }

        public static void LoadChildScenes(List<Scene> loadedSubscenes = null) {
            if (SubdividedSceneTracker.TryGet(out SubdividedScene scene, out _)) {
                new SubdividedScene.EditorAccess(scene).LoadAllScenes(true, loadedSubscenes);
            }
        }

        static void UnpackAnyPrefab(Transform transform) {
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject)) {
                PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            } else {
                for (int i = transform.childCount - 1; i >= 0; i--) {
                    var child = transform.GetChild(i);
                    UnpackAnyPrefab(child);
                }
            }
        }

        [MenuItem("TG/Build/Process Scenes/Process Selected Scenes", false, 0)]
        public static void ProcessSelectedScenes() {
            ProcessScenes(EditorScenesUtility.GetsSelectedScenesPaths());
        }

        [MenuItem("TG/Build/Process Scenes/Process Open Scenes", false, 0)]
        public static void ProcessOpenScenes() {
            ProcessScenes(EditorScenesUtility.GetCurrentlyOpenScenesPath());
        }

        [MenuItem("TG/Build/Process Scenes/Process All Scenes", false, 0)]
        public static void ProcessAllScenes() {
            ProcessScenes(GetAllScenes());
        }

        // Only for batch mode. Used via jenkins job
        static void BuildAddressablesOnly() {
            EnsureAddressablesDefine();
            if (HasArgument("strip_unused_addressables")) {
                if (!HasArgument("refresh_guid_cache")) {
                    // We need to force it to make stripping reliable
                    RefreshGUIDCache();
                }
                AddressablesCleaner.Cleaner.PerformBuildCleaning();
            }
            ProcessScenes(GetAllScenes());

            UnloadViews.UnloadAllView();
            PrepareAndBuildAddressables();

            EditorApplication.Exit(VerifyAddressablesBuild() ? 0 : 1);
        }

        public static bool IsDemo() => (Application.isBatchMode && HasArgument("gamemode_demo")) ||
                                       (!Application.isBatchMode && GameMode.IsDemo);
        
        [UsedImplicitly]
        [MenuItem("TG/Build/Prepare Environment", false, 0)]
        static void PrepareEnvironment() {
            EnsureBuildTarget();
            AddressableAssetSettings.CleanPlayerContent();
            PrepareDefines(EditorUserBuildSettings.activeBuildTarget);

            if (IsDemo()) {
                PrepareDemoScenes();
                if (!PlatformUtils.IsConsole) {
                    PrepareSteamDemoConfigs();
                    EditorApplication.ExecuteMenuItem("TG/Platform/Change Steam Id to Demo");
                }
            }

            if (HasArgument("process_hos_only")) {
                PrepareHosOnlyScenes();
            }

            if (PlatformUtils.IsXbox) {
                PrepareGameCoreConfig();
            }

            SaveGitInfo();
        }

        static void EnsureBuildTarget() {
            BuildTarget target = EditorUserBuildSettings.selectedBuildTargetGroup switch {
                BuildTargetGroup.Standalone when HasArgument("linux") => BuildTarget.StandaloneLinux64,
                BuildTargetGroup.Standalone => BuildTarget.StandaloneWindows64,
                BuildTargetGroup.GameCoreXboxOne => BuildTarget.GameCoreXboxOne,
                BuildTargetGroup.GameCoreXboxSeries => BuildTarget.GameCoreXboxSeries,
                BuildTargetGroup.PS5 => BuildTarget.PS5,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (EditorUserBuildSettings.activeBuildTarget != target) {
                throw new Exception($"Build target mismatch! Active build target is: {EditorUserBuildSettings.activeBuildTarget}. Requested build target is: {target}");
            }
        }

        static void PrepareDefines(BuildTarget buildTarget) {
            var targetGroup = BuildTargetToGroup(buildTarget);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var defines);

            bool updated = false;
            updated |= ArrayUtils.AddUnique(ref defines, "ADDRESSABLES_BUILD");
            if (IsDemo()) {
                updated |= ArrayUtils.AddUnique(ref defines, "AR_GAMEMODE_DEMO");
            }

            if (updated) {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
            }
        }

        static void AddScenesProcessedDefine() {
            var targetGroup = BuildTargetToGroup(EditorUserBuildSettings.activeBuildTarget);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var defines);

            if (ArrayUtils.AddUnique(ref defines, "SCENES_PROCESSED")) {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
            }
        }

        static void AddArchivesProducedDefine() {
            var targetGroup = BuildTargetToGroup(EditorUserBuildSettings.activeBuildTarget);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var defines);

            if (ArrayUtils.AddUnique(ref defines, "ARCHIVES_PRODUCED")) {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
            }
        }

        [MenuItem("TG/Build/Prepare GameMode/Demo/Scenes")]
        static void PrepareDemoScenes() {
            string[] allowedScenes = {
                "Prologue_Jail",
                "Prologue_Wyrdness",
                "Prologue_Beach",
                "Dungeon_ResRoom",
                "TitleScreen",
            };
            var group = AddressableHelper.FindGroup(SceneService.ScenesGroup);
            foreach (var entry in group.entries.ToArray()) {
                if (Array.IndexOf(allowedScenes, entry.address) == -1) {
                    group.RemoveAssetEntry(entry);
                }
            }

            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TG/Build/Prepare HoS Only")]
        static void PrepareHosOnlyScenes() {
            var hosScene = SceneReference.ByName("CampaignMap_HOS");
            var prologueScene = SceneReference.ByName("Prologue_Jail");
            var resChamberScene = SceneReference.ByName("Dungeon_ResRoom");
            
            var sceneCache = ScenesCache.Get;
            var group = AddressableHelper.FindGroup(SceneService.ScenesGroup);
            foreach (var entry in group.entries.ToArray()) {
                if (entry.address == "TitleScreen") {
                    continue;
                }

                var scene = SceneReference.ByAddressable(new ARAssetReference(entry.address));
                var region = sceneCache.GetOpenWorldRegion(scene);
                if (region != hosScene && region != prologueScene && scene != resChamberScene) {
                    group.RemoveAssetEntry(entry);
                }
            }

            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
        }

        static void PrepareGameCoreConfig() {
            string config = null;
            if (HasArgument("dev1")) {
                Log.Important?.Info("Chosen DEV 1 xbox config file");
                config = XboxDevConfig1;
            } else if (HasArgument("dev2")) {
                Log.Important?.Info("Chosen DEV 2 xbox config file");
                config = XboxDevConfig2;
            } else if (HasArgument("dev3")) {
                Log.Important?.Info("Chosen DEV 3 xbox config file");
                config = XboxDevConfig3;
            } else if (HasArgument("dev4")) {
                Log.Important?.Info("Chosen DEV 4 xbox config file");
                config = XboxDevConfig4;
            } else if (HasArgument("gamemode_demo")) {
                Log.Important?.Info("Chosen DEMO xbox config file");
                config = XboxDemoConfig;
            }

            if (config != null) {
                Log.Important?.Info($"Copying xbox config file '{config}' to '{XboxConfig}'");
                File.Copy(config, XboxConfig, true);
            }
        }

        static void PrepareSteamDemoConfigs() {
            File.Copy(SteamDemoAppVdf, SteamAppVdfPath, true);
            File.Copy(SteamDemoDepotVdf, SteamDepotVdfPath, true);
        }

        static void CheckDefines() {
#if ADDRESSABLES_BUILD
            Log.Critical?.Error("ADDRESSABLES_BUILD is set");
#else
            Log.Important?.Info("ADDRESSABLES_BUILD is not set");
#endif
        }

        static void EnsureAddressablesDefine() {
#if ADDRESSABLES_BUILD
            Log.Critical?.Error("ADDRESSABLES_BUILD is set");
#else
            throw new Exception("ADDRESSABLES_BUILD is not set");
#endif
        }

        [MenuItem("TG/Build/Save Git Info")]
        static void SaveGitInfo() {
            var gitBranch = GitBranchName();
            var gitCommit = GitUtils.GetCommitHash();

            using var versionControlInfo = new FileStream(GitDebugData.InfoPath, FileMode.Create);
            using var streamWriter = new StreamWriter(versionControlInfo);

            streamWriter.Write($"{gitBranch}|{gitCommit}");
        }

        static string GitBranchName() {
            var injectedBranchArray = ExtractArguments("Branch:") ?? Array.Empty<string>();
            return injectedBranchArray.FirstOrFallback(GitUtils.GetBranchName());
        }

        // -- Queries
        public static bool IsSubsceneByPath(string path) => path.Contains("Scenes/CampaignMap_") && path.Contains("Subscenes/");
        static bool IsSubdividedSceneMother() => Object.FindAnyObjectByType<SubdividedScene>() != null;
        static bool IsSubdividedSceneChild() => !IsSubdividedSceneMother() && Object.FindAnyObjectByType<SubdividedSceneChild>() != null;
        static bool IsChildScene() => IsSubdividedSceneChild();

        [MenuItem("CONTEXT/SceneAsset/Test for invalid references", false, -3000)]
        static void FindInvalidReferencesForScene(MenuCommand command) {
            var sceneAsset = (SceneAsset)command.context;

            var buildSettings = new BuildSettings {
                group = BuildTargetGroup.Standalone, target = BuildTarget.StandaloneWindows64,
            };
            var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            var usageTags = new BuildUsageTagSet();
            var usageCache = new BuildUsageCache();
            var sceneInfo =
                ContentBuildInterface.CalculatePlayerDependenciesForScene(scenePath, buildSettings, usageTags,
                    usageCache);
            if (sceneInfo.includedTypes.Any(static t => t == null)) {
                Log.Important?.Error($"{sceneAsset.name} <color=red>has invalid references</color>");
            } else {
                Log.Important?.Error($"{sceneAsset.name} <color=green>is correct</color>");
            }
        }
    }

    /// <summary>
    /// Ensures the loading and unloading of scenes for per scene operations
    /// </summary>
    public class SceneResources : IDisposable {
        public readonly Scene loadedScene;
        public List<Scene> loadedSubscenes;
        public readonly bool shouldSaveAdditionalScenes;

        public SceneResources(string sceneToLoad, bool shouldSaveAdditionalScenes) {
            loadedScene = EditorSceneManager.OpenScene(sceneToLoad);
            this.shouldSaveAdditionalScenes = shouldSaveAdditionalScenes;
            loadedSubscenes = new List<Scene>(0);
            BuildTools.LoadChildScenes(loadedSubscenes);
        }

        void ReleaseUnmanagedResources() {
            if (shouldSaveAdditionalScenes) {
                EditorSceneManager.SaveOpenScenes();
            } else {
                EditorSceneManager.SaveScene(loadedScene);
            }

            Resources.UnloadUnusedAssets();
        }

        public void Dispose() {
            ReleaseUnmanagedResources();
        }
    }

    public class BuildPathOption {
        readonly string _contentFolder;
        readonly string _executableName;

        public string BuildDirectory => $"{BuildTools.ContentBuilderPath}/{_contentFolder}";
        public string BuildPath => $"{BuildDirectory}/{_executableName}";

        public BuildPathOption(string contentFolder, string executableName) {
            _contentFolder = contentFolder;
            _executableName = executableName;
        }
    }
}