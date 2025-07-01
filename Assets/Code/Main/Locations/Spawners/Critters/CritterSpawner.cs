using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.ECS.Critters;
using Awaken.ECS.Critters.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.PackageUtilities.Collections;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using FMODUnity;
using TAO.VertexAnimation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using World = Unity.Entities.World;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    public partial class CritterSpawner : Element<Location>, IRefreshedByAttachment<CritterSpawnerAttachment> {
        public override ushort TypeForSerialization => SavedModels.CritterSpawner;

        const int ScheduledToRemoveEntitiesPreAllocateCount = 4;
        NativeArray<Entity> _allSpawnedCrittersEntities;
        TransformAccessArray _crittersTransforms;
        BlobAssetReference<CrittersPathPointsBlobData> _pathPointsBlobDataRef;
        Entity _crittersGroupEntity;
        EventReference _deathSoundRef;
        List<Critter> _critters;
        List<CritterDropData> _critterDropDatas;
        LocationTemplate _dropTemplate;
        Action<Location> onPickedUpCritterDrop;

        public void InitFromAttachment(CritterSpawnerAttachment spec, bool isRestored) {
            InitializeCrittersGroupEntity(spec);
            if (spec.UsePaths) {
                InitializeCrittersOnPaths(spec);
            } else {
                Debug.LogError($"{nameof(CritterSpawnerAttachment)} has disabled {spec.UsePaths}. Currently there is no other behaviours implemented so no critters will be spawned from spawner {spec.name}", spec);
            }

            _dropTemplate = spec.DropTemplateRef.Get<LocationTemplate>();
            _deathSoundRef = spec.DeathSound;

            _critterDropDatas = new(ScheduledToRemoveEntitiesPreAllocateCount);
            onPickedUpCritterDrop = OnPickedUpCritterDrop;
        }

        void InitializeCrittersGroupEntity(CritterSpawnerAttachment spec) {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _crittersGroupEntity = entityManager.CreateEntity(ComponentType.ReadWrite<CrittersGroupData>());
            entityManager.SetComponentData(_crittersGroupEntity, GetCrittersGroupData(spec));
        }

        static CrittersGroupData GetCrittersGroupData(CritterSpawnerAttachment spec) {
            var spawnerCenter = spec.transform.position;
            var spawnerRadius = spec.SpawnRadius;
            var cullingDistance = spec.MovementCullingDistance;
            return new CrittersGroupData(spawnerCenter, spawnerRadius, cullingDistance);
        }

        void InitializeCrittersOnPaths(CritterSpawnerAttachment spec) {
            _pathPointsBlobDataRef = spec.Paths.GetBlobAssetRef();

            int toSpawnCount = GetSpawnCount(spec, _pathPointsBlobDataRef.Value.PathsCount);
            var pathsStartPositions = CreatePathsStartPositions(spec, toSpawnCount, ARAlloc.Temp);

            CreateArraysForSpawnedCrittersGameObjects(toSpawnCount, out _crittersTransforms, out _critters, out var audioEmitters);
            SpawnCritterGameObjects(spec.CritterLogicPrefab, spec.gameObject.scene, pathsStartPositions,
                _crittersTransforms, _critters, audioEmitters, OnCritterDeath);

            _allSpawnedCrittersEntities = new NativeArray<Entity>(toSpawnCount, ARAlloc.Persistent);
            AllocationsTracker.CustomAllocation(_allSpawnedCrittersEntities);

            var minMaxScale = new float2(spec.CritterMinScale, spec.CritterMaxScale);
            var critterSharedData = new CritterGroupSharedData(spec.MovementParams, _pathPointsBlobDataRef, spec.Sounds, _crittersTransforms);

            SpawnCritterEntitiesOnPaths(spec.CritterVisualsPrefab, pathsStartPositions, minMaxScale, critterSharedData,
                _crittersGroupEntity, spec.gameObject, audioEmitters, _allSpawnedCrittersEntities.AsUnsafeSpan());

            CrittersTransformsSyncSystem transformsSyncSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CrittersTransformsSyncSystem>();
            transformsSyncSystem.AddCrittersGroupData(new(_allSpawnedCrittersEntities.AsUnsafeSpan(), _crittersTransforms, _crittersGroupEntity));

            pathsStartPositions.Dispose();
        }

        void OnCritterDeath(Critter critter) {
            var ecsWorld = World.DefaultGameObjectInjectionWorld;
            var entityManager = ecsWorld.EntityManager;
            var critterIndex = _critters.IndexOf(critter);
            var critterEntity = _allSpawnedCrittersEntities[critterIndex];

            var audioEmitter = entityManager.GetComponentObject<StudioEventEmitter>(critterEntity);
            // audioEmitter.Stop();
            // RuntimeManager.PlayOneShot(_deathSoundRef, audioEmitter.transform.position);

            var visualEntitiesBuffer = entityManager.GetBuffer<DrakeVisualEntity>(critterEntity, true);
            var visualEntitiesCountPerCritter = visualEntitiesBuffer.Length;
            var critterTransform = entityManager.GetComponentData<DrakeVisualEntitiesTransform>(critterEntity);

            Location spawnedDropLocation = _dropTemplate.SpawnLocation(critterTransform.position, critterTransform.rotation);
            var visualEntities = new NativeArray<Entity>(visualEntitiesCountPerCritter, ARAlloc.Persistent);
            for (int i = 0; i < visualEntitiesCountPerCritter; i++) {
                visualEntities[i] = visualEntitiesBuffer[i];
            }

            IEventListener onPickedUpListener = spawnedDropLocation.ListenTo(Location.Events.AnyItemPickedFromLocation, onPickedUpCritterDrop, this);
            _critterDropDatas.Add(new(spawnedDropLocation, onPickedUpListener, visualEntities));

            var ecb = ecsWorld.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>().CreateCommandBuffer();
            ecb.DestroyEntity(critterEntity);
            for (int i = 0; i < visualEntitiesCountPerCritter; i++) {
                ecb.SetComponent(visualEntitiesBuffer[i], new VA_AnimatorParams(0, CritterEntityData.ToDeathTransitionTime, CritterEntityData.DeathAnimationIndex));
            }
            UnityEngine.Object.Destroy(critter.gameObject);
        }

        void OnPickedUpCritterDrop(Location critterDropLocation) {
            int count = _critterDropDatas.Count;
            var ecbSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
            var ecb = ecbSystem.CreateCommandBuffer();
            for (int i = 0; i < count; i++) {
                var critterDropData = _critterDropDatas[i];
                if (critterDropData.location == critterDropLocation) {
                    Awaken.TG.MVC.World.EventSystem.TryDisposeListener(ref critterDropData.onPickedUpListener);
                    _critterDropDatas.RemoveAtSwapBack(i);
                    ecb.DestroyEntity(critterDropData.visualEntities);
                    critterDropData.visualEntities.Dispose();
                    critterDropLocation.Discard();
                    break;
                }
            }
        }

        static void SpawnCritterGameObjects(GameObject critterLogicPrefab, UnityEngine.SceneManagement.Scene targetScene, NativeArray<float3> pathsStartPositions,
            TransformAccessArray crittersTransforms, List<Critter> critters, List<StudioEventEmitter> audioEmitters, Action<Critter> onCritterDeath) {
            int toSpawnCount = pathsStartPositions.Length;
            var spawnedGameObjectsInstanceIds = new NativeArray<int>(toSpawnCount, ARAlloc.Temp);
            var spawnedTransformsIds = new NativeArray<int>(toSpawnCount, ARAlloc.Temp);
            GameObject.InstantiateGameObjects(critterLogicPrefab.GetInstanceID(), toSpawnCount, spawnedGameObjectsInstanceIds, spawnedTransformsIds, targetScene);
            for (int i = 0; i < toSpawnCount; i++) {
                var critterGameObject = Resources.InstanceIDToObject(spawnedGameObjectsInstanceIds[i]) as GameObject;
                var transform = critterGameObject?.transform;
                if (transform != null && critterGameObject != null && critterGameObject.TryGetComponent(out Critter critter)) {
                    transform.position = pathsStartPositions[i];
                    crittersTransforms.Add(transform);
                    critter.Setup(onCritterDeath);
                    critters.Add(critter);
                    audioEmitters.Add(critter.AudioEmitter);
                } else {
                    Log.Critical?.Error("critter transform, gameObject or critter component is null");
                    crittersTransforms.Add(null);
                    critters.Add(null);
                }
            }

            spawnedGameObjectsInstanceIds.Dispose();
            spawnedTransformsIds.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SpawnCritterEntitiesOnPaths(GameObject critterVisualsPrefab, NativeArray<float3> pathsStartPositions, float2 minMaxScale,
            CritterGroupSharedData groupSharedData,
            Entity crittersGroupEntity, GameObject spawnerGameObject, List<StudioEventEmitter> audioEmitters, UnsafeArray<Entity>.Span critterEntities) {
            if (critterVisualsPrefab == null) {
                Log.Critical?.Error($"Critter prefab is null on spawner {spawnerGameObject.name}", spawnerGameObject);
                return;
            }

            int count = pathsStartPositions.Length;
            if (count == 0) {
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            SpawnCrittersEntities(pathsStartPositions, entityManager, critterVisualsPrefab, spawnerGameObject, minMaxScale,
                groupSharedData, crittersGroupEntity, critterEntities);

            var minMaxSpeed = new float2(groupSharedData.movementParams.movementSpeedMin, groupSharedData.movementParams.movementSpeedMax);
            for (int critterIndex = 0; critterIndex < count; critterIndex++) {
                InitializeCritterEntityComponentsData(critterEntities[(uint)critterIndex], critterIndex, minMaxScale, minMaxSpeed, audioEmitters[critterIndex], entityManager);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SpawnCrittersEntities(NativeArray<float3> spawnPositions, EntityManager entityManager, GameObject critterVisualsPrefab, GameObject spawnerGameObject,
            float2 critterMinMaxScale, CritterGroupSharedData groupSharedData, Entity crittersGroupEntity, UnsafeArray<Entity>.Span critterEntities) {
            if (critterVisualsPrefab == null) {
                Log.Critical?.Error($"{nameof(critterVisualsPrefab)} is not assigned to {nameof(CritterSpawnerAttachment)} {spawnerGameObject.name}", spawnerGameObject);
                return;
            }

            var options = new IWithUnityRepresentation.Options {
                linkedLifetime = false,
                movable = false
            };

            var prefabInstanceDrakeLodGroup = DrakeRuntimeSpawning.InstantiatePrefab(critterVisualsPrefab, options);
            if (prefabInstanceDrakeLodGroup == null) {
                Log.Critical?.Error($"{nameof(critterVisualsPrefab)} {critterVisualsPrefab.name} does not have {nameof(DrakeLodGroup)} component", spawnerGameObject);
                return;
            }

            var prefabInstance = prefabInstanceDrakeLodGroup.gameObject;

            var tempDataEntity = entityManager.CreateEntity(DrakeRuntimeSpawning.DataEntityComponentTypes);
            DrakeRuntimeSpawning.CreateAndAddDrakeEntityPrefabs(prefabInstanceDrakeLodGroup, spawnerGameObject.scene, tempDataEntity, entityManager, ARAlloc.Temp,
                out var prefabVisualEntities);

            UnityEngine.Object.Destroy(prefabInstance);

            var prefabsDatas = entityManager.GetBuffer<DrakeStaticPrefabData>(tempDataEntity).AsNativeArray().CreateCopy(ARAlloc.Temp);
            entityManager.DestroyEntity(tempDataEntity);

            int drakeEntitiesPerInstanceCount = prefabVisualEntities.Length;

            var toSpawnCount = spawnPositions.Length;
            // component types without LinkedLifetimeRequest
            var critterEntityComponentTypes = new ReadOnlySpan<ComponentType>(CritterEntityData.CritterEntityComponentTypes, 0, CritterEntityData.CritterEntityComponentTypes.Length - 1);
            var critterEntityArchetype = entityManager.CreateArchetype(critterEntityComponentTypes);
            var critterPrefabEntity = entityManager.CreateEntity(critterEntityArchetype);
            InitializeCritterPrefabEntityComponentsData(critterPrefabEntity, groupSharedData, crittersGroupEntity, entityManager);

            var spawnedVisualEntities = new NativeArray<Entity>(drakeEntitiesPerInstanceCount, ARAlloc.Temp);
            for (int i = 0; i < toSpawnCount; i++) {
                float3 spawnPosition = spawnPositions[i];
                var critterEntity = entityManager.Instantiate(critterPrefabEntity);
                var random = new Unity.Mathematics.Random(math.hash(new int2(critterEntity.Index, critterEntity.Version)));
                var randomScale = random.NextFloat(critterMinMaxScale.x, critterMinMaxScale.y);
                quaternion randomRotation = random.NextQuaternionRotation();
                critterEntities[(uint)i] = critterEntity;
                DrakeRuntimeSpawning.SpawnDrakeEntities(in prefabVisualEntities, in prefabsDatas, in spawnPosition, in randomRotation, randomScale, ref entityManager, ref spawnedVisualEntities);
                var visualEntitiesBuffer = entityManager.GetBuffer<DrakeVisualEntity>(critterEntity).Reinterpret<Entity>();
                // also resizes buffer it to length of spawnedVisualEntities
                visualEntitiesBuffer.CopyFrom(spawnedVisualEntities);
            }
            spawnedVisualEntities.Dispose();

            entityManager.DestroyEntity(critterPrefabEntity);
            entityManager.DestroyEntity(prefabVisualEntities);
            prefabVisualEntities.Dispose();
            prefabsDatas.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InitializeCritterPrefabEntityComponentsData(Entity critterPrefabEntity, CritterGroupSharedData critterGroupSharedData, Entity crittersGroupEntity,
            EntityManager entityManager) {
            entityManager.SetSharedComponent(critterPrefabEntity, critterGroupSharedData);
            entityManager.SetSharedComponent(critterPrefabEntity, new CrittersGroupEntity(crittersGroupEntity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InitializeCritterEntityComponentsData(Entity critterEntity, int critterIndex,
            float2 minMaxScale, float2 minMaxSpeed, StudioEventEmitter audioEmitter, EntityManager entityManager) {
            var random = new Unity.Mathematics.Random(math.hash(new int2(critterEntity.Index, critterEntity.Version)));
            var randomScale = random.NextFloat(minMaxScale.x, minMaxScale.y);
            var randomSpeed = random.NextFloat(minMaxSpeed.x, minMaxSpeed.y);
            // Position will be set in CrittersWalkSystem
            entityManager.SetComponentData(critterEntity, new DrakeVisualEntitiesTransform(default, quaternion.identity, randomScale));
            entityManager.SetComponentData(critterEntity, new CritterMovementState(randomSpeed));
            entityManager.SetComponentData(critterEntity, new CritterAnimatorParams(0, 0, byte.MaxValue));
            entityManager.SetComponentData(critterEntity, new CritterIndexInGroup(critterIndex));
            entityManager.AddComponentObject(critterEntity, audioEmitter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CreateArraysForSpawnedCrittersGameObjects(int count, out TransformAccessArray crittersTransforms, out List<Critter> critters, out List<StudioEventEmitter> audioEmitters) {
            crittersTransforms = new TransformAccessArray(count);
            critters = new List<Critter>(count);
            audioEmitters = new List<StudioEventEmitter>(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static NativeArray<float3> CreatePathsStartPositions(CritterSpawnerAttachment spec, int toSpawnCount, Allocator allocator) {
            var pathsStartPositions = new NativeArray<float3>(toSpawnCount, allocator);
            for (int i = 0; i < toSpawnCount; i++) {
                pathsStartPositions[i] = spec.Paths[i][0].position;
            }

            return pathsStartPositions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetSpawnCount(CritterSpawnerAttachment spec, int pathsCount) {
            var toSpawnCount = spec.Count;
            if (toSpawnCount > pathsCount) {
                Log.Critical?.Error($"Trying to spawn more critters ({toSpawnCount}) than spawn positions available ({pathsCount}). Spawning {pathsCount} critters");
                toSpawnCount = pathsCount;
            }

            return toSpawnCount;
        }

#if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EDITOR_UpdateEntitiesData(CritterSpawnerAttachment spec) {
            if (_pathPointsBlobDataRef.IsCreated == false || _allSpawnedCrittersEntities.IsCreated == false || _crittersGroupEntity.Equals(Entity.Null)) {
                return;
            }
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.SetComponentData(_crittersGroupEntity, GetCrittersGroupData(spec));

            var spawnedCrittersQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<CritterGroupSharedData>(),
                ComponentType.ReadOnly<CrittersGroupEntity>());
            spawnedCrittersQuery.SetSharedComponentFilter(new CrittersGroupEntity(_crittersGroupEntity));

            var newSharedData = new CritterGroupSharedData(spec.MovementParams, _pathPointsBlobDataRef, spec.Sounds, _crittersTransforms);
            entityManager.SetSharedComponent(spawnedCrittersQuery, newSharedData);
            spawnedCrittersQuery.Dispose();
        }
#endif

        protected override void OnDiscard(bool fromDomainDrop) {
            CrittersTransformsSyncSystem transformsSyncSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CrittersTransformsSyncSystem>();
            if (transformsSyncSystem != null) {
                transformsSyncSystem.RemoveCritterGroupData(_crittersGroupEntity);
            }
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (_allSpawnedCrittersEntities.IsCreated) {
                entityManager.DestroyEntity(_allSpawnedCrittersEntities);
                _allSpawnedCrittersEntities.Dispose();
                AllocationsTracker.CustomFree(_allSpawnedCrittersEntities);
            }

            if (_pathPointsBlobDataRef.IsCreated) {
                _pathPointsBlobDataRef.Dispose();
            }

            if (_crittersTransforms.isCreated) {
                _crittersTransforms.Dispose();
            }

            if (_critterDropDatas != null) {
                for (int i = 0; i < _critterDropDatas.Count; i++) {
                    _critterDropDatas[i].visualEntities.Dispose();
                }
                _critterDropDatas = null;
            }
            _critters = null;
            _dropTemplate = null;
            onPickedUpCritterDrop = null;
            base.OnDiscard(fromDomainDrop);
        }

        struct CritterDropData {
            public Location location;
            public IEventListener onPickedUpListener;
            public NativeArray<Entity> visualEntities;

            public CritterDropData(Location critterDropLocation, IEventListener onPickedUpListener, NativeArray<Entity> visualEntities) {
                this.location = critterDropLocation;
                this.onPickedUpListener = onPickedUpListener;
                this.visualEntities = visualEntities;
            }
        }
    }
}