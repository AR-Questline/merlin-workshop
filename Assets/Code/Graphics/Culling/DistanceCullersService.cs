using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Graphics.Culling {
    public class DistanceCullersService : IService {
        readonly Dictionary<int, DistanceCuller> _cullerByScene = new();
        readonly HashSet<int> _scenesWhichGeneratedError = new();
        
        public Dictionary<int, DistanceCuller>.ValueCollection Cullers => _cullerByScene.Values;

        // Cullables
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(GameObject gameObject) {
            if (TryGetDistanceCuller(gameObject, out var culler)) {
                culler.RegisterLocationPrefab(gameObject);
            } else {
                var scene = gameObject.scene;
                var sceneHandle = scene.handle;
                if (!_scenesWhichGeneratedError.Contains(sceneHandle)) {
                    Log.Minor?.Warning($"No distance culler for scene: {scene.name}");
                    _scenesWhichGeneratedError.Add(sceneHandle);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(DistanceCullerRenderer rendererData) {
            if (TryGetDistanceCuller(rendererData.gameObject, out var culler)) {
                culler.Unregister(rendererData);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryGetDistanceCuller(GameObject gameObject, out DistanceCuller distanceCuller) {
            return TryGetDistanceCuller(gameObject.scene, out distanceCuller);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryGetDistanceCuller(UnityEngine.SceneManagement.Scene scene, out DistanceCuller distanceCuller) {
            return _cullerByScene.TryGetValue(scene.handle, out distanceCuller);
        }

        // === Distance cullers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(DistanceCuller distanceCuller, UnityEngine.SceneManagement.Scene scene) {
            _cullerByScene[scene.handle] = distanceCuller;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(DistanceCuller distanceCuller) {
            var toRemove = _cullerByScene.Where(p => p.Value == distanceCuller).Select(static p => p.Key).ToArray();
            foreach (var key in toRemove) {
                _cullerByScene.Remove(key);
            }
        }

        // === Operations
        public void BiasChanged() {
            _cullerByScene.Values.ForEach(static c => c.BiasChanged());
        }

        // === Debug
        public bool TryGetAny(out DistanceCuller distanceCuller) {
            distanceCuller = null;
            if (_cullerByScene.Count <= 0) {
                return false;
            }
            distanceCuller = _cullerByScene.Values.First();
            return distanceCuller;
        }
    }
}
