using System.Collections.Generic;
using System.Threading;
using Awaken.Kandra;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Character.Features {
    public abstract partial class MaterialBasedBodyFeature : BodyFeature {
        CancellationTokenSource _releasedDuringSpawnCancellationTokenSource;
        // Hack: We dont own GameObject so we can not rely on it being alive at discard time,
        // so we need to keep track of all possibly created by us materials
        readonly HashSet<RenderersMarkers.KandraMarker> _usedMarkers = new();

        protected abstract RendererMarkerMaterialType TargetMaterialType { get; }

        protected bool _alreadyInitialized;

        public sealed override async UniTask Spawn() {
#if UNITY_EDITOR
            if (_releasedDuringSpawnCancellationTokenSource != null) {
                Log.Important?.Error("Spawn called while already spawning: " + Features.SafeGameObject?.HierarchyPath(), Features.SafeGameObject);
                return;
            }
            
            if (_alreadyInitialized) {
                Log.Important?.Error("Spawn called while already spawned: " + Features.SafeGameObject?.HierarchyPath(), Features.SafeGameObject);
                return;
            }
#endif
            
            _releasedDuringSpawnCancellationTokenSource = new CancellationTokenSource();
            var token = _releasedDuringSpawnCancellationTokenSource.Token;
            await Initialize().AttachExternalCancellation(token);
            if (token.IsCancellationRequested) {
                return;
            }
            _releasedDuringSpawnCancellationTokenSource = null;
            
            if (Features.HasBeenDiscarded) {
#if UNITY_EDITOR
                Log.Important?.Error("Resources were not correctly released !!: " + Features.SafeGameObject?.HierarchyPath(), Features.SafeGameObject);
#endif
                return;
            }
            _alreadyInitialized = true;
            
            var skinMarker = Features.GameObject.GetComponentInChildren<RenderersMarkers>(true);
            if (skinMarker == null) {
                Log.Minor?.Error($"SkinRendererMarker not found in {Features.GameObject.name}", Features.GameObject);
            }

            foreach (var kandraMarker in skinMarker.KandraMarkers) {
                if (!kandraMarker.MaterialType.HasCommonBitsFast(TargetMaterialType)) {
                    continue;
                }
                kandraMarker.Renderer.EnsureInitialized();
#if UNITY_EDITOR
                var renderingMaterials = kandraMarker.Renderer.rendererData.RenderingMaterials;
                if (renderingMaterials == null) {
                    Log.Minor?.Error($"KandraRenderer is not initialized properly {kandraMarker.Renderer}", kandraMarker.Renderer);
                    continue;
                }
                if (renderingMaterials.Length <= kandraMarker.Index || kandraMarker.Index < 0) {
                    Log.Minor?.Error($"KandraRenderer {kandraMarker.Renderer} don't have material with index {kandraMarker.Index}", kandraMarker.Renderer);
                }
#endif
                Material material = kandraMarker.Renderer.UseInstancedMaterial(kandraMarker.Index);
                _usedMarkers.Add(kandraMarker);
                ApplyModifications(material, kandraMarker.Renderer);
            }
        }

        public sealed override UniTask Release(bool prettySwap = false) {
            _releasedDuringSpawnCancellationTokenSource?.Cancel();
            _releasedDuringSpawnCancellationTokenSource = null;
            
            foreach (var kandraMarker in _usedMarkers) {
                var renderer = kandraMarker.Renderer;
                if (!prettySwap) CleanupModification(renderer.rendererData.RenderingMaterials[kandraMarker.Index], renderer);
                kandraMarker.Renderer.UseOriginalMaterial(kandraMarker.Index);
            }
            _usedMarkers.Clear();
            FinalizeCleanup();
            _alreadyInitialized = false;
            return UniTask.CompletedTask;
        }

        protected virtual UniTask Initialize() {
            return UniTask.CompletedTask;
        }

        protected abstract void ApplyModifications(Material material, KandraRenderer renderer);
        protected abstract void CleanupModification(Material material, KandraRenderer renderer);
        protected abstract void FinalizeCleanup();
    }
}