using System.Runtime.CompilerServices;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.Utility.Collections;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Elevator {
    [UpdateInGroup(typeof(ElevatorSystemGroup))]
    [UpdateAfter(typeof(MoveElevatorChainsSystem))]
    [RequireMatchingQueriesForUpdate] [BurstCompile]
    public partial class GenerateElevatorChainsSystem : SystemBase {
        protected override void OnUpdate() {
            UnsafeList<PrefabsDatas> prefabsDatasList = default;
            UnsafeList<RemoveChainEntitiesData> removeChainsDatas = default;

            foreach (var (elevatorChainDataRef, chainPrefabsBuffer, drakeMeshMaterialComponentsBuffer) in SystemAPI.Query<
                         RefRW<ElevatorChainData>, DynamicBuffer<DrakeEntityPrefab>, DynamicBuffer<DrakeStaticPrefabData>>()) {
                var elevatorChainData = elevatorChainDataRef.ValueRO;
                int chainsLevelsToSpawnCount = GetChainLevelsToSpawnCount(in elevatorChainData);
                if (chainsLevelsToSpawnCount == 0) {
                    continue;
                }

                if (chainsLevelsToSpawnCount > 0) {
                    if (prefabsDatasList.IsCreated == false) {
                        prefabsDatasList = new UnsafeList<PrefabsDatas>(1, ARAlloc.Temp);
                    }

                    var chainsPrefabs = chainPrefabsBuffer.AsNativeArray().Reinterpret<Entity>().CreateCopy(ARAlloc.Temp);
                    var drakeStaticPrefabDatas = drakeMeshMaterialComponentsBuffer.AsNativeArray().CreateCopy(ARAlloc.Temp);
                    int prefabsCount = chainsPrefabs.Length;
                    Assert.AreEqual(prefabsCount, drakeStaticPrefabDatas.Length);

                    var platformCurrentPosition = new float3(
                        elevatorChainData.platformPositionXZ.x,
                        elevatorChainData.platformCurrentPositionY,
                        elevatorChainData.platformPositionXZ.y);
                    prefabsDatasList.Add(new PrefabsDatas(chainsPrefabs, drakeStaticPrefabDatas, elevatorChainData.chainRootsPositions,
                        elevatorChainData.spawnedEntities,
                        platformCurrentPosition, elevatorChainData.platformRotation, elevatorChainData.spawnedChainLevelsCount,
                        chainsLevelsToSpawnCount, elevatorChainData.finalSingleChainHeight));
                } else {
                    if (removeChainsDatas.IsCreated == false) {
                        removeChainsDatas = new UnsafeList<RemoveChainEntitiesData>(1, ARAlloc.Temp);
                    }

                    removeChainsDatas.Add(new RemoveChainEntitiesData(elevatorChainData.spawnedEntities,
                        elevatorChainData.spawnedChainLevelsCount, elevatorChainData.chainRootsPositions.Length,
                        chainPrefabsBuffer.Length, -chainsLevelsToSpawnCount));
                }

                elevatorChainDataRef.ValueRW.spawnedChainLevelsCount += chainsLevelsToSpawnCount;
            }

            var entityManager = EntityManager;
            if (prefabsDatasList.IsCreated) {
                for (int i = 0; i < prefabsDatasList.Length; i++) {
                    var prefabsDatas = prefabsDatasList[i];
                    SpawnChains(ref prefabsDatas, ref entityManager);
                    prefabsDatas.prefabs.Dispose();
                    prefabsDatas.datas.Dispose();
                }
                prefabsDatasList.Dispose();
            }

            if (removeChainsDatas.IsCreated) {
                for (int i = 0; i < removeChainsDatas.Length; i++) {
                    RemoveChainEntities(removeChainsDatas[i], ref entityManager);
                }
                removeChainsDatas.Dispose();
            }
        }

        [BurstCompile]
        static void SpawnChains(ref PrefabsDatas prefabsDatas, ref EntityManager entityManager) {
            int lastLevelToSpawn = prefabsDatas.spawnedChainLevelsCount + prefabsDatas.levelsToSpawnCount;
            for (int chainLevel = prefabsDatas.spawnedChainLevelsCount; chainLevel < lastLevelToSpawn; chainLevel++) {
                SpawnChainLevel(ref prefabsDatas, in chainLevel, ref entityManager);
            }
        }

        [BurstCompile]
        static void SpawnChainLevel(ref PrefabsDatas prefabsDatas, in int chainLevel,
            ref EntityManager entityManager) {
            int chainsRootsCount = prefabsDatas.chainRootLocalPositions.Length;
            var chainLevelHeightOffset = (prefabsDatas.singleChainHeight * chainLevel);
            var drakeEntitiesPerInstanceCount = prefabsDatas.prefabs.Length;
            for (int i = 0; i < chainsRootsCount; i++) {
                var chainPosition = prefabsDatas.platformCurrentPosition + prefabsDatas.chainRootLocalPositions[i];
                chainPosition.y += chainLevelHeightOffset;
                var spawnedEntitiesPrevCount = prefabsDatas.spawnedEntities.Length;
                prefabsDatas.spawnedEntities.ResizeUninitialized(spawnedEntitiesPrevCount + drakeEntitiesPerInstanceCount);
                var spawnedEntities = prefabsDatas.spawnedEntities.AsArray().GetSubArray(spawnedEntitiesPrevCount, drakeEntitiesPerInstanceCount);
                DrakeRuntimeSpawning.SpawnDrakeEntities(in prefabsDatas.prefabs, in prefabsDatas.datas, in chainPosition, in prefabsDatas.platformRotation, 1,
                    ref entityManager, ref spawnedEntities);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RemoveChainEntities(RemoveChainEntitiesData data, ref EntityManager entityManager) {
            var spawnedChainLevelsCount = data.spawnedChainLevelsCount;
            ref var spawnedEntities = ref data.spawnedEntities;
            var spawnedEntitiesCount = spawnedChainLevelsCount * data.chainRootsCount * data.chainPrefabsCount;
            Assert.AreEqual(spawnedEntitiesCount, spawnedEntities.Length);
            int entitiesToRemoveCount = data.chainLevelsToRemoveCount * data.chainRootsCount * data.chainPrefabsCount;
            Assert.IsTrue(entitiesToRemoveCount >= 0);
            Assert.IsTrue(entitiesToRemoveCount <= spawnedEntitiesCount);
            int startIndex = spawnedEntitiesCount - entitiesToRemoveCount;
            entityManager.DestroyEntity(spawnedEntities.AsArray().GetSubArray(startIndex, entitiesToRemoveCount));
            spawnedEntities.RemoveRange(startIndex, entitiesToRemoveCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetChainLevelsToSpawnCount(in ElevatorChainData elevatorChainData) {
            var totalChainHeight = math.max(elevatorChainData.maxChainY - elevatorChainData.platformCurrentPositionY, 0);
            int wantedChainCount = (int)math.ceil(totalChainHeight / elevatorChainData.finalSingleChainHeight);
            return wantedChainCount - elevatorChainData.spawnedChainLevelsCount;
        }

        struct PrefabsDatas {
            public NativeArray<Entity> prefabs;
            public NativeArray<DrakeStaticPrefabData> datas;
            public NativeArray<float3> chainRootLocalPositions;
            public NativeList<Entity> spawnedEntities;
            public quaternion platformRotation;
            public float3 platformCurrentPosition;
            public int spawnedChainLevelsCount;
            public int levelsToSpawnCount;
            public float singleChainHeight;

            public PrefabsDatas(NativeArray<Entity> prefabs, NativeArray<DrakeStaticPrefabData> datas, NativeArray<float3> chainRootLocalPositions,
                NativeList<Entity> spawnedEntities, float3 platformCurrentPosition, quaternion platformRotation, int spawnedChainLevelsCount,
                int levelsToSpawnCount, float singleChainHeight) {
                this.prefabs = prefabs;
                this.datas = datas;
                this.chainRootLocalPositions = chainRootLocalPositions;
                this.spawnedEntities = spawnedEntities;
                this.platformCurrentPosition = platformCurrentPosition;
                this.platformRotation = platformRotation;
                this.spawnedChainLevelsCount = spawnedChainLevelsCount;
                this.levelsToSpawnCount = levelsToSpawnCount;
                this.singleChainHeight = singleChainHeight;
            }
        }

        struct RemoveChainEntitiesData {
            public NativeList<Entity> spawnedEntities;
            public int spawnedChainLevelsCount;
            public int chainRootsCount;
            public int chainPrefabsCount;
            public int chainLevelsToRemoveCount;

            public RemoveChainEntitiesData(NativeList<Entity> spawnedEntities, int spawnedChainLevelsCount, int chainRootsCount,
                int chainPrefabsCount, int chainLevelsToRemoveCount) {
                this.spawnedEntities = spawnedEntities;
                this.spawnedChainLevelsCount = spawnedChainLevelsCount;
                this.chainRootsCount = chainRootsCount;
                this.chainPrefabsCount = chainPrefabsCount;
                this.chainLevelsToRemoveCount = chainLevelsToRemoveCount;
            }
        }
    }
}