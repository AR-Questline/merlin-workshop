using System.IO;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public class DrakeSceneBaker : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.DrakeBuild;
        public override bool canProcessSceneInIsolation => true;

        public static void ClearDrakeLibraryAssets() {
            var directoryPath = DrakeMergedRenderersLoading.BakingDirectoryPath;
            if (Directory.Exists(directoryPath)) {
                Directory.Delete(directoryPath, true);
            }
        }

#if !SCENES_PROCESSED
        [InitializeOnLoadMethod]
        static void PlaymodeWatcher() {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode) {
                    ClearDrakeLibraryAssets();
                }
            };
        }
#endif

        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            var drakeStatics = GameObjects.FindComponentsByTypeInScene<IDrakeStaticBakeable>(scene, true);
            foreach (var drake in drakeStatics) {
                drake.BakeStatic();
                TryToSetRepresentationOptionsFromProvider(drake);
                EditorUtility.SetDirty((MonoBehaviour)drake);
            }
            var mergedDrakes = GameObjects.FindComponentsByTypeInScene<DrakeMergedRenderersRoot>(scene, false);
            foreach (var mergedDrake in mergedDrakes) {
                mergedDrake.Bake();
            }
        }

        static void TryToSetRepresentationOptionsFromProvider(IDrakeStaticBakeable drake) {
            var combinedOptions = new IWithUnityRepresentation.Options();
            
            var providers = drake.gameObject.GetComponentsInParent<IDrakeRepresentationOptionsProvider>();
            foreach (var provider in providers) {
                if (provider is { ProvideRepresentationOptions: true }) {
                    CombineRepresentationOptions(ref combinedOptions, provider.GetRepresentationOptions());
                }
            }
            
            drake.SetUnityRepresentation(combinedOptions);
        }
        
        static void CombineRepresentationOptions(ref IWithUnityRepresentation.Options combinedOptions, IWithUnityRepresentation.Options otherOptions) {
            if (combinedOptions.linkedLifetime == null || otherOptions.linkedLifetime == true) {
                combinedOptions.linkedLifetime = otherOptions.linkedLifetime;
            }
                    
            if (combinedOptions.movable == null || otherOptions.movable == true) {
                combinedOptions.movable = otherOptions.movable;
            }
                    
            if (combinedOptions.requiresEntitiesAccess == null || otherOptions.requiresEntitiesAccess == true) {
                combinedOptions.requiresEntitiesAccess = otherOptions.requiresEntitiesAccess;
            }
        }
    }
}
