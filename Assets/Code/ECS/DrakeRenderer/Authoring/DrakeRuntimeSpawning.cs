using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Systems {
    [BurstCompile]
    public static class DrakeRuntimeSpawning {
        public static readonly ComponentType[] DataEntityComponentTypes = new[] {
            ComponentType.ReadWrite<DrakeEntityPrefab>(), ComponentType.ReadWrite<DrakeStaticPrefabData>()
        };
        
        public static DrakeLodGroup InstantiatePrefab(GameObject prefab, in IWithUnityRepresentation.Options options) {
            var prefabInstance = Object.Instantiate(prefab);
            if (prefabInstance.TryGetComponent(out DrakeLodGroup prefabInstanceDrakeLodGroup) == false) {
                Log.Important?.Error($"{prefab.name} prefab does not have {nameof(DrakeLodGroup)}");
                return null;
            }

            prefabInstanceDrakeLodGroup.SetUnityRepresentation(options);
            foreach (var drakeMeshRenderer in prefabInstanceDrakeLodGroup.Renderers) {
                if (drakeMeshRenderer == null) {
                    continue;
                }
                drakeMeshRenderer.SetUnityRepresentation(options);
            }
            
            return prefabInstanceDrakeLodGroup;
        }
        
        public static void CreateAndAddDrakeEntityPrefabs(DrakeLodGroup prefabInstanceDrakeLodGroup, UnityEngine.SceneManagement.Scene scene,
            Entity dataEntity, EntityManager entityManager, Allocator allocator, out NativeArray<Entity> prefabsEntities) {
            var ecb = new EntityCommandBuffer(ARAlloc.Temp);
            var drakeRendererManager = DrakeRendererManager.Instance;
            var drakeEntitiesFromEcb = ecb.AddBuffer<DrakeEntityPrefab>(dataEntity).Reinterpret<Entity>();
            ecb.AddBuffer<DrakeStaticPrefabData>(dataEntity);
            drakeRendererManager.Register(
                prefabInstanceDrakeLodGroup, scene, ecb, drakeEntitiesFromEcb);
            int drakeEntitiesCount = drakeEntitiesFromEcb.Length;
            for (int i = 0; i < drakeEntitiesCount; i++) {
                ecb.AddComponent<Prefab>(drakeEntitiesFromEcb[i]);
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
            prefabsEntities = new NativeArray<Entity>(drakeEntitiesCount, allocator);
            prefabsEntities.CopyFrom(entityManager.GetBuffer<DrakeEntityPrefab>(dataEntity).AsNativeArray().Reinterpret<Entity>());
            var drakeStaticPrefabsDatasBuffer = entityManager.GetBuffer<DrakeStaticPrefabData>(dataEntity);
            for (int i = 0; i < drakeEntitiesCount; i++) {
                var drakeEntity = prefabsEntities[i];
                var position = entityManager.GetComponentData<LocalToWorld>(drakeEntity).Position;
                var worldRenderBounds = entityManager.GetComponentData<WorldRenderBounds>(drakeEntity);
                var worldBoundsOffset = worldRenderBounds.Value.Center - position;
                var lodWorldReferencePoint = entityManager.GetComponentData<LODWorldReferencePoint>(drakeEntity);
                var lodWorldReferencePointOffset = lodWorldReferencePoint.Value - position;
                drakeStaticPrefabsDatasBuffer.Add(new DrakeStaticPrefabData() {
                    drakeMeshMaterial = entityManager.GetComponentData<DrakeMeshMaterialComponent>(prefabsEntities[i]),
                    worldBoundsCenterOffset = worldBoundsOffset,
                    worldBoundsExtents = worldRenderBounds.Value.Extents,
                    lodWorldReferencePointOffset = lodWorldReferencePointOffset
                });
            }
        }
        
        [BurstCompile]
        public static void SpawnDrakeEntities(in NativeArray<Entity> prefabs, in NativeArray<DrakeStaticPrefabData> datas,
            in float3 position, in quaternion rotation, float scale, ref EntityManager entityManager, ref NativeArray<Entity> spawnedEntities) {
            var prefabsCount = prefabs.Length;
            for (int i = 0; i < prefabsCount; i++) {
                var prefab = prefabs[i];
                var prefabData = datas[i];
                var instance = entityManager.Instantiate(prefab);
                entityManager.AddComponentData(instance, prefabData.drakeMeshMaterial);
                entityManager.SetComponentData(instance, new LocalToWorld() {
                    Value = float4x4.TRS(position, rotation, scale)
                });
                var worldBoundsCenter = position + prefabData.worldBoundsCenterOffset;
                var worldBoundsExtents = prefabData.worldBoundsExtents;
                var worldBoundsAABB = new AABB() {
                    Center = worldBoundsCenter,
                    Extents = worldBoundsExtents
                };
                entityManager.SetComponentData(instance, new WorldRenderBounds() { Value = worldBoundsAABB });
                var lodWorldReferencePoint = position + prefabData.lodWorldReferencePointOffset;
                entityManager.SetComponentData(instance, new LODWorldReferencePoint() { Value = lodWorldReferencePoint });
                spawnedEntities[i] = instance;
            }
        }
    }
}