using System;
using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Graphics.Previews {
    public interface IWithRenderersToPreview {
        GameObject PreviewParent { get; }
        bool IsValid => PreviewParent != null;
        string DisablePreviewKey { get; }

        bool TryGetRenderers(out MeshRenderer[] meshRenderer, out SkinnedMeshRenderer[] skinnedMeshRenderers, out IPreviewDataProvider[] providers);
        
        bool TryGetDrawMeshData(Matrix4x4 parentLocalToWorld, List<DrawMeshDatum> drawMeshData) {
            if (!TryGetRenderers(out var meshRenderers, out var skinnedMeshRenderers, out var providers)) {
                return false;
            }

            drawMeshData.EnsureCapacity(meshRenderers.Length+skinnedMeshRenderers.Length+providers.Length);

            foreach (var renderer in meshRenderers) {
                var datum = new DrawMeshDatum {
                    localBounds = renderer.bounds,
                    layer = renderer.gameObject.layer,
                    localToWorld = parentLocalToWorld*renderer.localToWorldMatrix,
                    materials = renderer.sharedMaterials,
                    mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
                };
                drawMeshData.Add(datum);
            }

            foreach (var renderer in skinnedMeshRenderers) {
                var datum = new DrawMeshDatum {
                    localBounds = renderer.bounds,
                    layer = renderer.gameObject.layer,
                    localToWorld = parentLocalToWorld*renderer.localToWorldMatrix,
                    materials = renderer.sharedMaterials,
                    mesh = renderer.sharedMesh,
                };
                drawMeshData.Add(datum);
            }

            foreach (var provider in providers) {
#if UNITY_EDITOR
                var datum = provider.EDITOR_GetDrawMeshDatum();
                datum.localToWorld = parentLocalToWorld*datum.localToWorld;
                drawMeshData.Add(datum);
#endif
            }

            return true;
        }

        void RegisterToPreview() {
            EditorRenderersPreview.Register(this);
        }
        
        void UnregisterFromPreview() {
            EditorRenderersPreview.Unregister(this);
        }

#if UNITY_EDITOR
        bool CanBeRenderInPrefabStage(UnityEditor.SceneManagement.PrefabStage prefabStage) {
            return prefabStage.IsPartOfPrefabContents(PreviewParent);
        }
#endif
    }

    public interface IPrefabHandleToPreview : IWithRenderersToPreview {
        bool TryLoadPrefabToPreview(out ARAsyncOperationHandle<GameObject> handle);

        bool IWithRenderersToPreview.TryGetRenderers(out MeshRenderer[] meshRenderer, out SkinnedMeshRenderer[] skinnedMeshRenderers, out IPreviewDataProvider[] providers) {
            if (TryLoadPrefabToPreview(out var handle) && handle.Result is { } prefab) {
                meshRenderer = prefab.GetComponentsInChildren<MeshRenderer>();
                skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                providers = prefab.GetComponentsInChildren<IPreviewDataProvider>();
                return true;
            } else {
                meshRenderer = Array.Empty<MeshRenderer>();
                skinnedMeshRenderers = Array.Empty<SkinnedMeshRenderer>();
                providers = Array.Empty<IPreviewDataProvider>();
                return false;
            }
        }
    }
    
    public interface IAssetReferenceToPreview : IPrefabHandleToPreview {
        ARAssetReference ToPreviewReference { get; }

        bool IPrefabHandleToPreview.TryLoadPrefabToPreview(out ARAsyncOperationHandle<GameObject> handle) {
            try {
                handle = ToPreviewReference.LoadAsset<GameObject>();
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
                handle = default;
                return false;
            }
        }
    }
}
