using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Unity.Mathematics;

namespace Awaken.TG.Graphics.MapServices {
    public partial class MapService : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.MapService;

        public Domain Domain => Domain.Gameplay;

        [Saved] List<SceneReference> _visitedScenes = new();
        [Saved] Dictionary<SceneReference, MapMemory> _memoryByScene = new();
        Dictionary<SceneReference, InstanceData> _instanceByScene = new();

        public bool RemoveOnDomainChange() {
            foreach (var instance in _instanceByScene.Values) {
                instance.instance.Dispose();
            }
            return true;
        }

        public bool WasVisited(SceneReference scene) {
            return _visitedScenes.Contains(scene);
        }

        public void Visit(SceneReference scene) {
            _visitedScenes.AddUnique(scene);
        }
        
        public bool TryGetFogOfWar(SceneReference scene, out FogOfWar instance) {
            if (_instanceByScene.TryGetValue(scene, out var data)) {
                instance = data.instance;
                return instance != null;
            } else {
                instance = null;
                return false;
            }
        }

        public FogOfWar LoadFogOfWar(SceneReference scene) {
            if (_instanceByScene.TryGetValue(scene, out var data) == false) {
                data = new InstanceData {
                    instance = new FogOfWar(scene, GetMemoryFor(scene)),
                    refCount = 0,
                };
                data.instance.ReadMemory();
                _instanceByScene[scene] = data;
            }
            data.refCount++;
            return data.instance;
        }

        public void ReleaseFogOfWar(FogOfWar instance) {
            if (_instanceByScene.TryGetValue(instance.Scene, out var data)) {
                data.refCount--;
                if (data.refCount == 0) {
                    data.instance.WriteMemory();
                    data.instance.Dispose();
                    data.instance = null;
                    _instanceByScene.Remove(instance.Scene);
                }
            }
        }
        
        MapMemory GetMemoryFor(SceneReference scene) {
            if (!_memoryByScene.TryGetValue(scene, out var data)) {
                _memoryByScene[scene] = data = new MapMemory {
                    visitedPixels = Array.Empty<float2>()
                };
            }
            return data;
        }
        
        public override void OnBeforeSerialize() {
            foreach (var instance in _instanceByScene.Values) {
                instance.instance.WriteMemory();
            }
        }

        public override void OnAfterDeserialize() {
            foreach (var instance in _instanceByScene.Values) {
                instance.instance.ReadMemory();
            }
        }
        
        class InstanceData {
            public FogOfWar instance;
            public int refCount;
        }
    }
}