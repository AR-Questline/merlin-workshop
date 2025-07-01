using System.IO;
using Awaken.CommonInterfaces;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Files;
using Awaken.Utility.Maths.Data;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.LeshyRenderer {
    public class LeshyNavMeshBakingPreparation : INavMeshBakingPreparer.IReversible {
        GameObject _colliderParent;
        
        public LeshyNavMeshBakingPreparation(LeshyManager manager, LeshyPrefabs prefabs) {
            if (!File.Exists(manager.MatricesPath) || !File.Exists(manager.CatalogPath)) {
                Log.Important?.Warning($"Not baked Leshy for scene {manager.gameObject.scene.name}.");
                return;
            }
            var catalog = LeshyCells.GetCellCatalog(manager.CatalogPath, ARAlloc.Temp);
            var matricesFile = AsyncReadManager.OpenFileAsync(manager.MatricesPath);
            _colliderParent = new GameObject("leshy colliders");
            var colliderParentTransform = _colliderParent.transform;
            foreach (var cell in catalog) {
                ref readonly var authoring = ref prefabs.Prefabs[cell.prefabId];
                bool hasCollider = authoring is { HasCollider: true, prefabType: LeshyPrefabs.PrefabType.Tree or LeshyPrefabs.PrefabType.LargeObject };
                if (!hasCollider) {
                    continue;
                }
                var matrices = FileRead.ToNewBuffer<SmallTransform>(matricesFile, cell.matricesOffset, cell.instancesCount, ARAlloc.Temp);
                var collider = authoring.colliders;
                foreach (var matrix in matrices) {
                    var go = Object.Instantiate(collider, matrix.position, matrix.rotation, colliderParentTransform);
                    go.transform.localScale = (float3)matrix.scale;
                }
                matrices.Dispose();
            }
            matricesFile.Close().Complete();
            catalog.Dispose();
            AsyncReadManager.CloseCachedFileAsync(manager.MatricesPath).Complete();
        }
        
        public void Revert() {
            Object.DestroyImmediate(_colliderParent);
            _colliderParent = null;
        }
    }
}