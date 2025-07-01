using System;
using System.Threading;
using Awaken.Kandra;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.TG.Assets;
using Awaken.Utility.Collections;
using Awaken.Utility.SerializableTypeReference;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// Marker script for renderers that are available to be dissolved.
    /// </summary>
    public class DissolveAbleRendererKandraWrapper : IDissolveAbleRendererWrapper {
        Material[] _originalMaterials;
        Material[] _instancedMaterials;
        readonly DissolveAbleRenderer _dar;
        readonly KandraRenderer _renderer;
        CancellationTokenSource _cancellationTokenSource;
        ARAsyncOperationHandle<Material>[] _handles;

        public Material[] InstancedMaterialsForExternalModifications => _instancedMaterials ?? Array.Empty<Material>();
        public bool InDissolvableState { get; private set; }

        public DissolveAbleRendererKandraWrapper(DissolveAbleRenderer dar, KandraRenderer renderer) {
            _dar = dar;
            _renderer = renderer;
        }

        public void Init() {
            if (_renderer == null) {
                return;
            }
            _originalMaterials = _renderer.GetOriginalMaterials().CreateCopy();

            if (_dar.dontReplaceMaterials) {
                _dar.dissolveAbleMaterials = new Material[_originalMaterials.Length];
                for (int i = 0; i < _originalMaterials.Length; i++) {
                    _dar.dissolveAbleMaterials[i] = _originalMaterials[i];
                }
            }
        }
        
        public void Destroy() {
            if (_renderer && _dar.IsInDissolvableState) {
                RestoreToOriginalMaterials();
            }
            _originalMaterials = Array.Empty<Material>();
            if (_handles != null) {
                ReleaseHandles(ref _handles);
            }
        }

        public void ChangeToDissolveAble() {
            InDissolvableState = true;
            _cancellationTokenSource?.Cancel();
            _renderer.EnsureInitialized();
            if (_dar.dissolveAbleMaterials is { Length: > 0 }) {
                _renderer.ChangeOriginalMaterials(_dar.dissolveAbleMaterials);
                _instancedMaterials = _renderer.UseInstancedMaterials();
            } else {
                LoadAndChangeMaterials(_dar.dissolveAbleMaterialRefs).Forget();
            }
        }

        public void RestoreToOriginalMaterials() {
            InDissolvableState = false;
            _cancellationTokenSource?.Cancel();
            _renderer.UseOriginalMaterials();
            _renderer.ChangeOriginalMaterials(_originalMaterials);
            if (_handles != null) {
                ReleaseHandles(ref _handles);
            }
        }

        public void InitPropertyModification(SerializableTypeReference serializedType, float value) { }

        public void UpdateProperty(SerializableTypeReference serializedType, float value) {
            if (_instancedMaterials == null) {
                return;
            }
            var shaderNameId = MaterialOverrideUtils.GetPropertyID(serializedType);
            foreach (var material in _instancedMaterials) {
                if (material != null) {
                    material.SetFloat(shaderNameId, value);
                }
            }
        }
        
        public void FinishPropertyModification(SerializableTypeReference serializedType) { }

        async UniTaskVoid LoadAndChangeMaterials(ARAssetReference[] materialRefs) {
            var handles = new ARAsyncOperationHandle<Material>[materialRefs.Length];
            for (int i = 0; i < handles.Length; i++) {
                handles[i] = materialRefs[i].LoadAsset<Material>();
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
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
            
            var materials = new Material[handles.Length];
            for (int i = 0; i < materials.Length; i++) {
                if (handles[i].Result == null) {
                    ReleaseHandles(ref handles);
                    return;             
                }
                materials[i] = handles[i].Result;
            }
            
            _handles = handles;
            _renderer.ChangeOriginalMaterials(materials);
            _instancedMaterials = _renderer.UseInstancedMaterials();
        }
        
        void ReleaseHandles(ref ARAsyncOperationHandle<Material>[] handles) {
            foreach (var handle in handles) {
                handle.Release();
            }
            handles = null;
        }
    }
}