using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.ECS.DrakeRenderer.Systems {
    public static class DrakeRuntimeSpawning {
        public static readonly ComponentType[] DataEntityComponentTypes = new[] {
            ComponentType.ReadWrite<DrakeEntityPrefab>(), ComponentType.ReadWrite<DrakeStaticPrefabData>()
        };

        public static DrakeLodGroup InstantiatePrefab(GameObject prefab, in IWithUnityRepresentation.Options options) {
            throw new NotImplementedException();
        }

        public static void CreateAndAddDrakeEntityPrefabs(DrakeLodGroup prefabInstanceDrakeLodGroup, Scene scene,
            Entity dataEntity, EntityManager entityManager, Allocator allocator,
            out NativeArray<Entity> prefabsEntities) {
            throw new NotImplementedException();
        }

        public static void SpawnDrakeEntities(in NativeArray<Entity> prefabs,
            in NativeArray<DrakeStaticPrefabData> datas,
            in float3 position, in quaternion rotation, float scale, ref EntityManager entityManager,
            ref NativeArray<Entity> spawnedEntities) {
            throw new NotImplementedException();
        }
    }
}