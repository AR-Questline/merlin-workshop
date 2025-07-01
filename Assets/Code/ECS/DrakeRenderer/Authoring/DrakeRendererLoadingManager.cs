using System.Collections.Generic;
using System.Resources;
using Awaken.CommonInterfaces;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UniversalProfiling;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    [Il2CppEagerStaticClassConstruction]
    public class DrakeRendererLoadingManager : IAddressableMaterialsLoadingEventsProvider {
        const int PreAllocSize = 512;
        static readonly UniversalProfilerMarker LoadMeshMarker = new("DrakeRendererLoadingManager.LoadMesh");
        static readonly UniversalProfilerMarker LoadMaterialMarker = new("DrakeRendererLoadingManager.LoadMaterial");

        Dictionary<string, ushort> _meshKeyToIndex = new(PreAllocSize*2);
        Dictionary<string, ushort> _materialKeyToIndex = new(PreAllocSize*2);

        Dictionary<int, ushort> _runtimeMaterialToIndex = new(PreAllocSize);

        [ShowInInspector] List<AddressableLoadingData<Mesh>> _meshLoadingData = new(PreAllocSize);
        [ShowInInspector] List<AddressableLoadingData<Material>> _materialLoadingData = new(PreAllocSize);
        public List<AddressableLoadingData<Mesh>> MeshLoadingData => _meshLoadingData;
        public List<AddressableLoadingData<Material>> MaterialLoadingData => _materialLoadingData;
        public event System.Action<string> OnStartedLoadingMaterial;
        public event System.Action<string> OnUnloadingMaterial;
        public event System.Action<string, Material> OnLoadedMaterial;

        public DrakeRendererLoadingManager() {
            AddressableMaterialsLoadingEventsProviders.AddProvider(this);
        }

        public bool StartLoadingMesh(ushort meshIndex) {
            var started = false;
            var meshLoadingData = _meshLoadingData[meshIndex];
            if (meshLoadingData.counter == 0) {
                LoadMeshMarker.Begin();
                var loading = Addressables.LoadAssetAsync<Mesh>(meshLoadingData.key);
                meshLoadingData.loadingHandle = loading;
                LoadMeshMarker.End();
                started = true;
            }
            meshLoadingData.counter++;
            _meshLoadingData[meshIndex] = meshLoadingData;
            return started;
        }

        public bool StartLoadingMaterial(ushort materialIndex) {
            var started = false;
            var materialLoadingData = _materialLoadingData[materialIndex];
            if (materialLoadingData.counter == 0) {
                OnStartedLoadingMaterial?.Invoke(materialLoadingData.key);
                LoadMaterialMarker.Begin();
                var loading = Addressables.LoadAssetAsync<Material>(materialLoadingData.key);
                materialLoadingData.loadingHandle = loading;
                LoadMaterialMarker.End();
                started = true;
            }
            materialLoadingData.counter++;
            _materialLoadingData[materialIndex] = materialLoadingData;
            return started;
        }

        public bool WillReleaseMesh(ushort meshIndex) {
            return _meshLoadingData[meshIndex].counter == 1;
        }

        public void UnloadMesh(ushort meshIndex) {
            var meshLoadingData = _meshLoadingData[meshIndex];
            meshLoadingData.counter--;
            if (meshLoadingData.counter == 0) {
                var handle = meshLoadingData.loadingHandle;
                Addressables.Release(handle);
                meshLoadingData.loadingHandle = default;
            }
            _meshLoadingData[meshIndex] = meshLoadingData;
        }

        public bool WillReleaseMaterial(ushort materialIndex) {
            return _materialLoadingData[materialIndex].counter == 1;
        }

        public bool MaterialReleased(ushort materialIndex) {
            return _materialLoadingData[materialIndex].counter == 0;
        }

        public void UnloadMaterial(ushort materialIndex) {
            var materialLoadingData = _materialLoadingData[materialIndex];
            materialLoadingData.counter--;
            if (materialLoadingData.counter == 0) {
                OnUnloadingMaterial?.Invoke(materialLoadingData.key);
                var handle = materialLoadingData.loadingHandle;
                Addressables.Release(handle);
                materialLoadingData.loadingHandle = default;
            }
            _materialLoadingData[materialIndex] = materialLoadingData;
        }

        public bool TryGetLoadedMesh(ushort index, out Mesh mesh) {
            var handle = _meshLoadingData[index].loadingHandle;
            if (handle.IsValid() && handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded) {
                mesh = handle.Result;
            } else {
                mesh = null;
            }

            return mesh != null;
        }

        public bool TryGetLoadedMaterial(ushort index, out string runtimeKey, out Material material) {
            var loadingData = _materialLoadingData[index];
            runtimeKey = loadingData.key;
            var handle = loadingData.loadingHandle;
            if (handle.IsValid() && handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded) {
                material = handle.Result;
            } else {
                material = null;
            }
            return material != null;
        }

        public ushort GetMeshIndex(string meshKey) {
            if (_meshKeyToIndex.TryGetValue(meshKey, out var index)) {
                return index;
            }
            index = (ushort)_meshLoadingData.Count;
            _meshLoadingData.Add(new AddressableLoadingData<Mesh>(meshKey));
            _meshKeyToIndex.Add(meshKey, index);
            return index;
        }
        
        public ushort GetMaterialIndex(string materialKey) {
            if (_materialKeyToIndex.TryGetValue(materialKey, out var index)) {
                return index;
            }
            index = (ushort)_materialLoadingData.Count;
            _materialLoadingData.Add(new AddressableLoadingData<Material>(materialKey));
            _materialKeyToIndex.Add(materialKey, index);
            return index;
        }

        public string GetMaterialKey(ushort materialIndex) {
            return _materialLoadingData[materialIndex].key;
        }

        public (ushort materialIndex, bool added) RegisterRuntimeMaterialIndex(Material material) {
            var materialHash = material.GetHashCode();
            if (_runtimeMaterialToIndex.TryGetValue(materialHash, out var index)) {

                var materialLoadingData = _materialLoadingData[index];
                materialLoadingData.counter++;
                _materialLoadingData[index] = materialLoadingData;

                return (index, false);
            } else {
                index = (ushort)_materialLoadingData.Count;

                var operation = new RuntimeMaterialOperation(material);

                var materialLoadingData = new AddressableLoadingData<Material>("Runtime");
                materialLoadingData.loadingHandle = Addressables.ResourceManager.StartOperation(operation, new AsyncOperationHandle());
                materialLoadingData.counter = 1;

                _materialLoadingData.Add(materialLoadingData);
                _runtimeMaterialToIndex.Add(materialHash, index);
                return (index, true);
            }
        }

        public ushort RemoveRuntimeMaterial(Material material) {
            var materialHash = material.GetHashCode();
            if (_runtimeMaterialToIndex.TryGetValue(materialHash, out var index)) {
                var materialLoadingData = _materialLoadingData[index];
                materialLoadingData.counter--;
                if (materialLoadingData.counter == 0) {
                    materialLoadingData.loadingHandle = default;
                }
                _materialLoadingData[index] = materialLoadingData;
                return index;
            } else {
                Log.Critical?.Error($"Material {material} was not found in runtime materials", material);
                return ushort.MaxValue;
            }
        }

        public void DropRuntimeMaterial(Material material) {
            var materialHash = material.GetHashCode();
            if (_runtimeMaterialToIndex.TryGetValue(materialHash, out var index)) {
                if (_materialLoadingData[index].counter == 0) {
                    _runtimeMaterialToIndex.Remove(materialHash);
                } else {
                    Log.Critical?.Error($"Material {material} is still in use", material);
                }
            } else {
                Log.Critical?.Error($"Material {material} was not found in runtime materials", material);
            }
        }

        public void CallMaterialLoaded(string runtimeKey, Material material) {
            OnLoadedMaterial?.Invoke(runtimeKey, material);
        }

        public struct AddressableLoadingData<T> where T : Object {
            [ShowInInspector] public readonly string key;
            public AsyncOperationHandle<T> loadingHandle;
            [ShowInInspector] public ushort counter;

            [ShowInInspector] bool IsLoaded => counter > 0 && loadingHandle.IsDone;

            public AddressableLoadingData(string key) {
                this.key = key;
                loadingHandle = default;
                counter = 0;
            }
        }

        public class RuntimeMaterialOperation : AsyncOperationBase<Material> {
            // === Fields
            Material _material;
            float _progress;

            protected override float Progress => _progress;

            // === Constructor
            public RuntimeMaterialOperation(Material material) {
                _material = material;
            }

            protected override void Execute() {
                _progress = 1f;

                Result = _material;
                Complete(Result, true, "");
            }
        }
    }
}
