using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.General.Caches {
    [Serializable]
    public abstract class SceneSource {
        [HideInInspector] 
        public SceneReference sceneRef;
        [HideInInspector] 
        public SceneReference motherScene;
        [PropertyOrder(-1)]
        public string scenePath;

        string _sceneName;
        [ShowInInspector, PropertyOrder(-2)]
        public string SceneName => string.IsNullOrEmpty(_sceneName) ? _sceneName = sceneRef.Name : _sceneName;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public string OpenWorldRegion => ScenesCache.Get.GetOpenWorldRegion(sceneRef)?.Name;
        
        public GameObject SceneGameObject {
            get {
                var scene = sceneRef.LoadedScene;
                return scene.isLoaded ? GameObjects.GetGameObject(scenePath, scene) : null;
            }
        }

        protected SceneSource(GameObject go) {
#if UNITY_EDITOR
            InitSceneLocations();
            string path = _sceneResourceLocations.FirstOrDefault(loc => loc.InternalId.EndsWith($"/{go.scene.name}.unity"))?.InternalId;
            
            if (path != null) {
                string sceneGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                this.sceneRef = SceneReference.ByAddressable(new ARAssetReference(sceneGuid));
                this.motherScene = ScenesCache.Get.TryGetMainSceneOfTheSubscene(this.sceneRef) ?? this.sceneRef;
            } else {
                Log.Critical?.Error($"Scene wasn't found in addressables {go.scene.name}");
            }

            this.scenePath = go.PathInSceneHierarchy(true);
#else
            throw new InvalidOperationException();
#endif
        }

        protected SceneSource(SceneReference sceneRef, string path) {
            this.sceneRef = sceneRef;
            scenePath = path;
        }

        List<IResourceLocation> _sceneResourceLocations;
        void InitSceneLocations() {
            if (_sceneResourceLocations == null) {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(SceneService.ScenesLabel);
                _sceneResourceLocations = locationsHandle.WaitForCompletion().ToList();
            }
        }

#if UNITY_EDITOR
        [Button("Jump To Source"), HorizontalGroup("EditorTools"), PropertyOrder(1000)]
        void JumpToSingleScene() {
            JumpToSource();
        }
        [Button("As Additive"), HorizontalGroup("EditorTools", width: 88), PropertyOrder(1001)] 
        void JumpToSourceAsAdditive() {
            JumpToSource(true);
        }
        
        void JumpToSource(bool asAdditive = false) {
            if (!sceneRef.IsSet || string.IsNullOrEmpty(scenePath)) {
                return;
            }

            if (!sceneRef.LoadedScene.isLoaded) {
                string sceneGuid = new SceneReference.EditorAccess(sceneRef).Reference.Address;
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, asAdditive ? UnityEditor.SceneManagement.OpenSceneMode.Additive : UnityEditor.SceneManagement.OpenSceneMode.Single);
            }

            GameObject go = SceneGameObject;
            if (go != null) {
                UnityEditor.EditorGUIUtility.PingObject(go);
            } else {
                Log.Important?.Error($"Couldn't find this Game Object: {scenePath}. Probably was moved or deleted.");
            }
        }
#endif
    }
}