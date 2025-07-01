using System;
using System.Linq;
using Awaken.TG.Assets.Modding;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Encapsulation of logic that allows targeting scenes in addressables using either their guids or their names (location key)
    /// Works with both Unity and our serialization systems
    /// All systems that are passing around scenes should use this wrapper to target them
    /// </summary>
    [Serializable, InlineProperty]
    public partial class SceneReference : IEquatable<SceneReference> {
        public ushort TypeForSerialization => SavedTypes.SceneReference;

        // === Serialized Fields
        [SerializeField, Saved]
#if UNITY_EDITOR
        [ARAssetReferenceSettings(new[] {typeof(UnityEditor.SceneAsset)}, true, AddressableGroup.Scenes, labels: new[] {SceneService.ScenesLabel})]
#endif
        ARAssetReference reference;

        [Saved, NonSerialized] string _addressableLocationKey;
        [NonSerialized] string _name;

        // === Properties
        public string Name => string.IsNullOrEmpty(_name) ? _name = RetrieveName() : _name;

        public string DomainName => Name;
        public bool IsSet => !string.IsNullOrWhiteSpace(Name);
        /// <summary>
        /// Returns default if scene is not loaded
        /// </summary>
        public Scene LoadedScene => SceneManager.GetSceneByName(Name);
        public Domain Domain => Domain.Scene(this);
        public bool IsAdditive => CommonReferences.Get.SceneConfigs.IsAdditive(this);

        // === Constructors
        public static SceneReference ByName(string name) {
            return new() {_addressableLocationKey = name};
        }

        public static SceneReference ByScene(Scene scene) => ByName(scene.name);

        public static SceneReference ByAddressable(ARAssetReference reference) {
            return new() {reference = reference};
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve] 
        public SceneReference() { }
        
        // === Operations
        public IMapScene RetrieveMapScene() => RetrieveMapScene<IMapScene>();
        public T RetrieveMapScene<T>() where T : IMapScene {
            foreach (var root in LoadedScene.GetRootGameObjects()) {
                if (root.TryGetComponent(out T mapScene)) {
                    return mapScene;
                }
            }
            return default;
        }
        
        public IScene RetrieveSceneForUnloading() {
            return LoadedScene.GetRootGameObjects()
                .Select(r => r.GetComponent<IMapScene>() ?? r.GetComponent<TitleScreen>() as IScene)
                .FirstOrDefault(scene => scene != null);
        }
        
        string RetrieveName() {
            return string.IsNullOrEmpty(_addressableLocationKey)
                ? ModService.GetAddressableLocation(reference?.RuntimeKey)?.PrimaryKey
                : _addressableLocationKey;
        }

        // === Equality members
        public bool Equals(SceneReference other) {
            return Name == other?.Name;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SceneReference) obj);
        }

        public override int GetHashCode() {
            return Name != null ? Name.GetHashCode() : 0;
        }

        // === Operators
        public static bool operator ==(SceneReference left, SceneReference right) {
            return Equals(left, right);
        }

        public static bool operator !=(SceneReference left, SceneReference right) {
            return !Equals(left, right);
        }

        public static SerializationAccessor Serialization(SceneReference reference) => new(reference);
        public readonly struct SerializationAccessor {
            readonly SceneReference _reference;
            
            public SerializationAccessor(SceneReference reference) {
                _reference = reference;
            }
            
            public ref ARAssetReference Reference => ref _reference.reference;
        }

#if UNITY_EDITOR
        public readonly struct EditorAccess {
            readonly SceneReference _reference;

            public EditorAccess(SceneReference reference) {
                _reference = reference;
            }

            public ARAssetReference Reference => _reference.reference;
            public Scene LoadedScene => (_reference != null && _reference.IsSet) ? _reference.LoadedScene : default;
            
            public Scene LoadScene(UnityEditor.SceneManagement.OpenSceneMode mode = UnityEditor.SceneManagement.OpenSceneMode.Additive) {
                if (_reference.IsSet == false) {
                    return default;
                }
                var scene = _reference.LoadedScene;
                var assetRef = _reference.reference;
                if (!scene.IsValid() && assetRef != null) {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(assetRef.Address);
                    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,
                        mode);
                }
                VerifySubscene(scene);
                return scene;
            }
            
            public void UnloadScene(bool withSave) {
                if (_reference.IsSet == false) {
                    return;
                }
                var scene = _reference.LoadedScene;
                if (scene.IsValid()) {
                    bool close = true;
                    if (scene.isDirty && withSave) {
                        int result = UnityEditor.EditorUtility.DisplayDialogComplex(
                            "Unloading Scene",
                            $"Scene {scene.name} has unsaved changes. What do you want to do?",
                            "Save and unload",
                            "Don't save and unload",
                            "Don't unload"
                        );
                        if (result == 0) {
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                        } else if (result == 2) {
                            close = false;
                        }
                    }
                    if (close) {
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            
            static void VerifySubscene(Scene scene) {
                if (HasMapScene(scene)) {
                    Log.Important?.Error($"{scene.name} has MapScene object. As subscene it shouldn't");
                }

                if (HasNavMesh(scene)) {
                    Log.Important?.Error($"{scene.name} has NavMesh object. As subscene it shouldn't.");
                }

                static bool HasMapScene(Scene scene) {
                    return scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<IMapScene>() != null);
                }

                static bool HasNavMesh(Scene scene) {
                    return scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<AstarPath>() != null);
                }
            }
        }
#endif
    }
}