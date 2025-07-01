using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.Utility.Collections;
using Awaken.Utility.SerializableTypeReference;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// Marker script for renderers that are available to be dissolved.
    /// </summary>
    public class DissolveAbleRendererDrakeWrapper : IDissolveAbleRendererWrapper {
        string[] _originalRuntimeKeys;
        Material[] _instancedMaterials;
        CancellationTokenSource _cts;
        readonly DissolveAbleRenderer _dar;
        readonly LinkedEntitiesAccess _linkedEntitiesAccess;

        public Material[] InstancedMaterialsForExternalModifications => Array.Empty<Material>();
        public bool InDissolvableState { get; private set; }
        bool HasRuntimeMaterials => _dar.dissolveAbleMaterials is { Length: > 0 };

        public DissolveAbleRendererDrakeWrapper(DissolveAbleRenderer dar, LinkedEntitiesAccess linkedEntitiesAccess) {
            _dar = dar;
            _linkedEntitiesAccess = linkedEntitiesAccess;
        }

        public void Init() {
            if (_linkedEntitiesAccess == null) {
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            var entityManager = world.EntityManager;
            var manager = DrakeRendererManager.Instance;
            var loadingManager = manager.LoadingManager;
            List<string> runtimeKeys = new();
            foreach (var entity in _linkedEntitiesAccess.LinkedEntities) {
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                var meshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(entity);
                loadingManager.TryGetLoadedMaterial(meshMaterial.materialIndex, out var runtimeKey, out var material);
                runtimeKeys.Add(runtimeKey);
            }
            
            _originalRuntimeKeys = runtimeKeys.ToArray();
        }

        public void Destroy() {
            if (_linkedEntitiesAccess && _dar.IsInDissolvableState) {
                RestoreToOriginalMaterials();
            }
            _originalRuntimeKeys = Array.Empty<string>();
        }

        public void ChangeToDissolveAble() {
            InDissolvableState = true;
            if (_dar.dontReplaceMaterials) {
                return;
            }
            _cts?.Cancel();
            if (HasRuntimeMaterials) {
                _instancedMaterials = _dar.dissolveAbleMaterials.CreateCopy();
                ChangeMaterials(_instancedMaterials);
                return;
            }
            LoadAndChangeMaterial(_dar.dissolveAbleMaterialRefs.Select(m => m.RuntimeKey).ToArray()).Forget();
        }

        public void RestoreToOriginalMaterials() {
            InDissolvableState = false;
            if (_dar.dontReplaceMaterials) {
                return;
            }
            _cts?.Cancel();
            RestoreMaterials().Forget();
        }
        
        public void InitPropertyModification(SerializableTypeReference serializedType, float value) {
            var materialOverride = new MaterialOverrideData(TypeManager.GetTypeIndex(serializedType), value);
            MaterialOverrideUtils.ApplyMaterialOverrides(_linkedEntitiesAccess, materialOverride);
        }

        public void UpdateProperty(SerializableTypeReference serializedType, float value) {
            var materialOverride = new MaterialOverrideData(TypeManager.GetTypeIndex(serializedType), value);
            MaterialOverrideUtils.ApplyMaterialOverrides(_linkedEntitiesAccess, materialOverride);
        }

        public void FinishPropertyModification(SerializableTypeReference serializedType) {
            MaterialOverrideUtils.RemoveMaterialOverrides(_linkedEntitiesAccess, serializedType.Type);
        }
        
        /// <summary>
        /// Used for replacing materials with materials from addressables
        /// </summary>
        async UniTask LoadAndChangeMaterial(string[] runtimeKeys, bool loadOriginalMaterials = false) {
            AsyncOperationHandle<Material>[] handles = new AsyncOperationHandle<Material>[runtimeKeys.Length];
            for (int i = 0; i < handles.Length; i++) {
                handles[i] = Addressables.LoadAssetAsync<Material>(runtimeKeys[i]);
            }
            
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            bool anyLoading;
            do {
                anyLoading = false;
                for (var i = 0u; (!anyLoading) & (i < handles.Length); i++) {
                    if (!handles[i].IsDone) {
                        anyLoading = true;
                    }
                }
                if (anyLoading) {
                    await UniTask.NextFrame();
                }
            }
            while ((token.IsCancellationRequested == false) & anyLoading);
            if (token.IsCancellationRequested) {
               ReleaseHandles(ref handles);
                return;
            }
            
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                ReleaseHandles(ref handles);
                return;
            }
            DrakeReplaceMaterialsUtils.ReplaceDrakeMaterials(world, _linkedEntitiesAccess, runtimeKeys);
            ReleaseHandles(ref handles);
        }

        /// <summary>
        /// Used for replacing materials with runtime materials
        /// </summary>
        void ChangeMaterials(Material[] materials) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            DrakeReplaceMaterialsUtils.ReplaceDrakeMaterials(world, _linkedEntitiesAccess, materials);
        }
        
        async UniTaskVoid RestoreMaterials() {
            if (_originalRuntimeKeys.Length == 0) {
                return;
            }
            await LoadAndChangeMaterial(_originalRuntimeKeys, true);
            if (_instancedMaterials != null) {
                DrakeReplaceMaterialsUtils.DestroyDrakeRuntimeMaterials(_instancedMaterials, true);
                _instancedMaterials = null;
            }
        }
        
        void ReleaseHandles(ref AsyncOperationHandle<Material>[] handles) {
            if (handles == null) {
                return;
            }
            foreach (var handle in handles) {
                handle.Release();
            }
            handles = null;
        }
    }
}