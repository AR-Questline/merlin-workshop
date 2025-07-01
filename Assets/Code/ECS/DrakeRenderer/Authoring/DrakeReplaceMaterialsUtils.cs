using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.ECS.Mipmaps.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public static class DrakeReplaceMaterialsUtils {
        public static void ReplaceDrakeMaterials(World world, LinkedEntitiesAccess linkedEntitiesAccess, string[] materialKeys) {
            var manager = DrakeRendererManager.Instance;
            var loadingManager = manager.LoadingManager;
            var componentsManager = manager.ComponentsManager;

            var materialIndices = new UnsafeArray<ushort>((uint)materialKeys.Length, ARAlloc.Temp);
            for (var i = 0u; i < materialKeys.Length; i++) {
                var materialKey = materialKeys[i];
                var materialIndex = loadingManager.GetMaterialIndex(materialKey);
                materialIndices[i] = materialIndex;

                var newMeshMaterial = new DrakeMeshMaterialComponent(0, materialIndex, 0);
                componentsManager.Register(newMeshMaterial);
            }

            ReplaceDrakeMaterials(linkedEntitiesAccess, world, materialIndices);

            materialIndices.Dispose();
        }

        public static void ReplaceDrakeMaterials(World world, LinkedEntitiesAccess linkedEntitiesAccess, Material[] runtimeMaterials) {
            var manager = DrakeRendererManager.Instance;
            var loadingManager = manager.LoadingManager;
            var componentsManager = manager.ComponentsManager;

            var materialIndices = new UnsafeArray<ushort>((uint)runtimeMaterials.Length, ARAlloc.Temp);
            for (var i = 0u; i < runtimeMaterials.Length; i++) {
                var (materialIndex, newAdded) = loadingManager.RegisterRuntimeMaterialIndex(runtimeMaterials[i]);
                materialIndices[i] = materialIndex;

                var newMeshMaterial = new DrakeMeshMaterialComponent(0, materialIndex, 0);
                componentsManager.Register(newMeshMaterial);

                if (newAdded) {
                    componentsManager.MarkLoadingRuntimeMaterial(materialIndex);
                }
            }

            componentsManager.UpdateLoadings();

            ReplaceDrakeMaterials(linkedEntitiesAccess, world, materialIndices);

            materialIndices.Dispose();
        }

        public static void DestroyDrakeRuntimeMaterials(Material[] runtimeMaterials, bool destroy) {
            var manager = DrakeRendererManager.Instance;
            var loadingManager = manager.LoadingManager;
            var componentsManager = manager.ComponentsManager;

            for (var i = 0u; i < runtimeMaterials.Length; i++) {
                var runtimeMaterial = runtimeMaterials[i];
                var runtimeMaterialIndex = loadingManager.RemoveRuntimeMaterial(runtimeMaterial);
                componentsManager.UnloadRuntimeMaterial(runtimeMaterialIndex);
                if (destroy) {
                    loadingManager.DropRuntimeMaterial(runtimeMaterial);
                    Object.Destroy(runtimeMaterial);
                }
            }
        }

        static void ReplaceDrakeMaterials(LinkedEntitiesAccess linkedEntitiesAccess, World world, in UnsafeArray<ushort>.Span materialIndices) {
            var manager = DrakeRendererManager.Instance;
            var componentsManager = manager.ComponentsManager;

            var loadingSystem = world.GetExistingSystemManaged<DrakeRendererLoadingSystem>();
            var entityManager = world.EntityManager;

            var waitingDrake = new NativeList<WaitingEntity>(ARAlloc.Temp);
            var entitiesAccess = linkedEntitiesAccess.LinkedEntities;
            for (var i = 0u; i < entitiesAccess.Length; i++) {
                var entity = entitiesAccess[i];
                if (!entityManager.HasComponent<DrakeMeshMaterialComponent>(entity)) {
                    continue;
                }
                var meshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(entity);
                var newMaterial = materialIndices[(uint)meshMaterial.submesh];
                var newMeshMaterial = new DrakeMeshMaterialComponent(meshMaterial.meshIndex, newMaterial, meshMaterial.submesh);

                // Drake states are:
                // 1) Unloaded
                // 2) Unloading requested
                // 3) Loading requested
                // 4) Loading
                // 5) Loaded

                var hasUnloadingRequest = entityManager.HasComponent<DrakeRendererUnloadRequestTag>(entity);
                var hasLoadingRequest = entityManager.HasComponent<DrakeRendererLoadRequestTag>(entity);
                var hasLoading = entityManager.HasComponent<DrakeRendererLoadingTag>(entity);
                var hasSpawned = entityManager.HasComponent<DrakeRendererSpawnedTag>(entity);

                var isUnloaded = !hasLoading && !hasSpawned;
                var isUnloadingRequested = hasUnloadingRequest && hasSpawned;
                var isLoadingRequested = hasLoadingRequest;
                var isLoading = hasLoading;
                var isLoaded = hasSpawned && !hasUnloadingRequest;

                if (isUnloaded) { // Just replace
                    entityManager.SetComponentData(entity, newMeshMaterial);
                } else if (isUnloadingRequested) { // Follow DrakeRendererLoadingSystem and after replace
                    manager.Unload(meshMaterial);
                    entityManager.RemoveComponent(entity, loadingSystem.ReleaseResourcesRemoveSet);
                    entityManager.SetComponentData(entity, newMeshMaterial);
                } else if (isLoadingRequested) { // Not started loading yet, just replace
                    entityManager.SetComponentData(entity, newMeshMaterial);
                } else if (isLoading) { // Stop ongoing loading, replace and start loading again
                    manager.Unload(meshMaterial, false);
                    entityManager.SetComponentData(entity, newMeshMaterial);
                    manager.StartLoading(newMeshMaterial);
                } else if (isLoaded) { // Start loading
                    manager.StartLoading(newMeshMaterial);
                    waitingDrake.Add(new WaitingEntity(entity, newMeshMaterial));
                } else {
                    Log.Critical?.Error($"Unknown state for {entity}", linkedEntitiesAccess);
                }
            }

            if (waitingDrake.Length > 0) {
                componentsManager.UpdateLoadings();

                foreach (var (entity, newMeshMaterial) in waitingDrake) {
                    var meshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(entity);
                    manager.Unload(meshMaterial);

                    if (!manager.TryGetMaterialMesh(newMeshMaterial,
                            out var meshMaterialInfo, out var mipmapsMaterialComponent, out var uvDistributionMetricComponent)) {
                        Log.Critical?.Error($"Failed to get material mesh info for {newMeshMaterial}, now we are in unknown state", linkedEntitiesAccess);
                    }

                    entityManager.SetComponentData(entity, newMeshMaterial);
                    entityManager.SetComponentData(entity, meshMaterialInfo);
                    entityManager.SetComponentData(entity, mipmapsMaterialComponent);
                    entityManager.SetComponentData(entity, uvDistributionMetricComponent);
                    entityManager.SetComponentData(entity, new MipmapsFactorComponent { value = float.MaxValue });
                }
            }

            waitingDrake.Dispose();
        }

        struct WaitingEntity {
            public Entity entity;
            public DrakeMeshMaterialComponent meshMaterial;

            public WaitingEntity(Entity entity, DrakeMeshMaterialComponent meshMaterial) {
                this.entity = entity;
                this.meshMaterial = meshMaterial;
            }

            public void Deconstruct(out Entity entity, out DrakeMeshMaterialComponent meshMaterial) {
                entity = this.entity;
                meshMaterial = this.meshMaterial;
            }
        }
    }
}
