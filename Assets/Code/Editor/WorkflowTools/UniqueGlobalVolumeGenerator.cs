using System.IO;
using System.Linq;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.WorkflowTools {
    public static class UniqueGlobalVolumeGenerator {
        [InitializeOnLoadMethod]
        public static void InitOnLoad() {
            // UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            // UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated += OnNewSceneCreated;
        }

        static void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) {
            if (!BuildSceneBaking.isBakingScenes) {
                HandleCorrectVolumeSetup(scene);
            }
        }

        static void OnNewSceneCreated(Scene scene, UnityEditor.SceneManagement.NewSceneSetup setup, UnityEditor.SceneManagement.NewSceneMode mode) {
            HandleCorrectVolumeSetup(scene);
        }
        
        static void HandleCorrectVolumeSetup(Scene scene) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (NotGameScene(scene)) return;

            Volume foundVolume = null;
            int volumeCount = 1;
            foreach (Volume volume in GameObjects.FindComponentsByTypeInScene<Volume>(scene, false).Where(v => v.isGlobal && v.priority == 0)) {
                if (foundVolume != null) {
                    if (volumeCount == 1) Log.Important?.Error("Multiple Global Volumes with priority 0 found in scene. Only one should be present.");
                    Log.Important?.Warning($"Volume {volumeCount++}: {volume.name}", volume);
                }

                foundVolume ??= volume;
            }
            
            if (foundVolume == null) {
                Log.Important?.Error($"No Global Volume found in scene {scene.name} with priority 0. Please add one");
                return;
            }
            
            string volumeIntendedName = "Volume_Global_" + scene.name;
            VolumeProfile volumeProfile = foundVolume.sharedProfile;
            bool anyChange = false;
            
            if (volumeProfile == null) {
                // Check if file already exists but changes to scene were not sent
                volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath(volumeIntendedName));
                if (volumeProfile != null) {
                    foundVolume.sharedProfile = volumeProfile;
                    EditorUtility.SetDirty(foundVolume);
                } else {
                    volumeProfile = CreateNewVolumeProfile(volumeIntendedName, foundVolume);
                }

                anyChange = true;
            } else {
                // Ensure that volume is unique for this scene
                GUIDCache.Load();
                string assetPath = AssetDatabase.GetAssetPath(volumeProfile);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (ShouldDuplicate(guid, volumeProfile.name, scene)) {
                    volumeProfile = DuplicateExistingProfile(volumeIntendedName, foundVolume);
                    anyChange = true;
                } else {
                    // Ensure that volume name is the same as the scene name
                    if (volumeProfile.name != volumeIntendedName) {
                        Log.Important?.Info($"Renaming Volume Profile for scene {volumeIntendedName} was: {volumeProfile.name}");
                        AssetDatabase.RenameAsset(assetPath, volumeIntendedName);
                        anyChange = true;
                    }
                    
                    // Ensure that all components are unique to this volume
                    for (int i = volumeProfile.components.Count - 1; i >= 0; i--) {
                        var component = volumeProfile.components[i];
                        if (component == null) {
                            volumeProfile.components.RemoveAt(i);
                            continue;
                        }
                        
                        if (AssetDatabase.GetAssetPath(component) == assetPath) {
                            continue;
                        }
                        anyChange = true;
                        
                        // duplicate component
                        var newComponent = Object.Instantiate(component);
                        newComponent.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                        volumeProfile.components[i] = newComponent;
                        CopyValuesToComponent(component, newComponent, false);
                        
                        AssetDatabase.AddObjectToAsset(newComponent, volumeProfile);
                    }
                }
                GUIDCache.Unload();
            }

            if (anyChange) {
                Log.Important?.Warning($"Global Volume setup for scene {scene.name} was updated", volumeProfile);
            }
        }

        static bool NotGameScene(Scene scene) {
            var mapScene = GameObjects.FindComponentByTypeInScene<MapScene>(scene, false);
            var additiveScene = GameObjects.FindComponentByTypeInScene<AdditiveScene>(scene, false);
            return mapScene == null && additiveScene == null;
        }

        static bool ShouldDuplicate(string guid, string profileName, Scene scene) {
            var dependants = GUIDCache.Instance.GetDependent(guid).Where(FilterResults).ToArray();
            // we cannot fully rely on dependants count, as there might be some other connections that are not in cache
            return (dependants.Length > 1 && !profileName.EndsWith(scene.name)) || CommonReferences.Get.SceneConfigs.AllScenes.Any(c => profileName.EndsWith(c.sceneName) && c.sceneName != scene.name);
        }

        static bool FilterResults(string arg) => !arg.EndsWith("GUIDSearchingCache.asset") && !arg.EndsWith(".meta");

        static VolumeProfile DuplicateExistingProfile(string volumeIntendedName, Volume foundVolume) {
            var volumeProfile = CreateVolumeProfileAtPath(volumeIntendedName, foundVolume.sharedProfile);
            Log.Important?.Info($"Duplicating Volume Profile for scene {volumeIntendedName} was: {volumeProfile.name}");
            
            foundVolume.sharedProfile = volumeProfile;
            EditorUtility.SetDirty(foundVolume);
            EditorSceneManager.SaveScenes(new[] {foundVolume.gameObject.scene});
            return volumeProfile;
        }

        static VolumeProfile CreateVolumeProfileAtPath(string name, VolumeProfile source) {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = name;
            AssetDatabase.CreateAsset(profile, ProfilePath(name));

            if (source != null) {
                foreach (var sourceComponent in source.components) {
                    var profileComponent = profile.Add(sourceComponent.GetType());
                    for (int i = 0; i < sourceComponent.parameters.Count; i++) {
                        profileComponent.parameters[i].overrideState = sourceComponent.parameters[i].overrideState;
                    }
                    CopyValuesToComponent(sourceComponent, profileComponent, true);
                    AssetDatabase.AddObjectToAsset(profileComponent, profile);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        static void CopyValuesToComponent(VolumeComponent component, VolumeComponent targetComponent, bool copyOnlyOverriddenParams) {
            if (targetComponent == null) {
                return;
            }

            for (int i = 0; i < component.parameters.Count; i++) {
                var param = component.parameters[i];
                if (copyOnlyOverriddenParams && !param.overrideState) {
                    continue;
                }
                var targetParam = targetComponent.parameters[i];
                targetParam.SetValue(param);
            }
        }

        static string ProfilePath(string volumeIntendedName) => "Assets/3DAssets/Lighting/Volumes/" + volumeIntendedName + ".asset";

        static VolumeProfile CreateNewVolumeProfile(string volumeIntendedName, Volume foundVolume) {
            Log.Important?.Info($"Creating new Volume Profile for scene {volumeIntendedName}");
            string volumeProfilePath = ProfilePath(volumeIntendedName);
            var newProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(newProfile, volumeProfilePath);
            foundVolume.sharedProfile = newProfile;
            EditorUtility.SetDirty(foundVolume);
            return newProfile;
        }
    }
}