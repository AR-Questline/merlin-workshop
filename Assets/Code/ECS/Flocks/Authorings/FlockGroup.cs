using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Systems;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using FMODUnity;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Flocks.Authorings {
    public class FlockGroup : MonoBehaviour {
        public const int MaxEntitiesCount = 300;

        [Header("Flocking Area")]
        [Tooltip("Extents of the box where the flock group target position can be generated")]
        public Vector3 areaExtents = new(20, 10, 20);

        [Tooltip("Extents of the box which defines how much spread out will be randomly generated positions for each individual flock entity")]
        public Vector3 positionVarianceExtent = new(6, 3, 6);

        [Tooltip("How often flock group target position will be updating")]
        public float2 targetPositionUpdateMinMax = new(0.5f, 5f);

        [Tooltip("Scaling of the flock entity mesh")]
        public float2 flockEntityMinMaxScale = new(1.3f, 1.5f);

        [Tooltip("Distance from player at which flock ecs systems will process this flock group. Will be increased if any flock entity max render distance is greater than this value")]
        [FormerlySerializedAs("minRenderDistance")] public float minimalSimulateDistance = 500f;

        public bool overrideEntitiesMaxRenderDistance;
        [EnableIf(nameof(overrideEntitiesMaxRenderDistance))] public float entitiesMaxRenderDistanceOverride = 300f;

        [Header("Behaviour")]
        [Tooltip("Chance for the flock entity to dive down when it reaches the old target before moving to the new target")]
        public float diveOnReachedTargetChance = 0.07f;

        [Tooltip("Max height difference the flock entity can dive. Will be chosen randomly from 0 to divMaxHeightDiff")]
        public float diveMaxHeightDiff = 5f;

        [Space(5)]
        [Tooltip("Max delay for the flock entity to wander around old target position when the new target position is generated. Will be chosen randomly from o to maxDelayForUsingNewFlockTarget")]
        public float maxDelayForUsingNewFlockTarget = 1.5f;

        [Header("Movement")]
        [Tooltip("Multiplier for turning the flock entity towards the target position")]
        public float2 steeringSpeedMultiplierMinMax = new(1, 2);

        [Tooltip("Forward movement speed (in units per second)")]
        public float2 movementSpeedMinMax = new(15, 25);

        [Tooltip("Forward movement acceleration speed (in units per second)")]
        public float movementAcceleration = 4;

        [Tooltip("Forward movement deceleration speed (in units per second)")]
        public float movementDeceleration = 4;

        [Space(5)]
        [Tooltip("Forward movement deceleration speed used when entity  (in units per second)")]
        public float maxDecelerationForReachRestPosition = 20;

        [Tooltip("Minimal forward movement speed to use when the flock entity is decelerating trying to reach rest position")]
        public float minSpeedForMovingToRestPosition = 2;

        [Tooltip("Multiplier for turning the flock entity towards the rest position. Probably should be higher than steeringSpeedMultiplier Max.")]
        public float toRestSteeringSpeedMult = 3;

        [Header("Avoidance")]
        public SteeringParams avoidanceSteeringParams = new(20, 0.2f, 0.1f);

        [Tooltip("Parameter p for function y = 1 - x^p which will be multiplier for steeringParams.maxRotationSpeed. X is normalized angle [0-180] between current direction and direction to obstacle. Makes so that the more directly towards obstacle entity is flying, the faster it rotates away from obstacle. the slower it rotates towards it, making entity to rotate fast to approximately correct direction and then slowly rotate to exactly correct direction. When p is 0-1: it is exponential towards zero (for the most of the time will be slow rotation), when > 1: it is logarithmic towards zero (for the most of the time will be fast rotation). The closer p is to 0, the quicker entity will stop rotating away from obstacle. The bigger p is, the faster entity will rotate away from obstacle.")]
        public float avoidanceSpeedMultiplierCurvePow = 0.5f;

        [Tooltip("Parameter which increases avoidance rotation speed (in radians per second) if entity is too close to obstacle, based on collider radius. Applied using formula: rotationSpeed += normalizedExceedingDistanceToObstacle^2 * parameter.")]
        public float avoidanceRotationSpeedAdditionWhenExceeding = 50;

        [Space(5)]
        public LayerMask avoidanceLayerMask = new LayerMask() { value = ~0 };

        [Tooltip("Radius defining lenght of up and down raycasts length. Also defines how close entity should be to the rest position to start moving directly towards rest position without slowly turning towards it. Can be viewed as a gizmos on the position this gameObject")]
        public float colliderRadius = 1;

        [Tooltip("Angle to side from forward direction for the forward raycasts. Can be viewed as a gizmos on the position this gameObject")]
        public float forwardCheckRayAngle = 25;

        [Tooltip("Forward raycasts length")]
        public float forwardCheckRayLength = 2.5f;

        [Header("Animation")]
        public FlyingFlockEntityAnimationsData animationsData = new(0, 1, 2, new float2(0.3f, 0.6f), 0.5f, true);

        [Header("Sounds")]
        public float2 flyingOrRestingSoundPlayDelayMinMax = new float2(4, 5);

        [Space(5)]
        [FoldoutGroup("Group Audio Events")] public EventReference groupFlyingEvent;

        [FoldoutGroup("Group Audio Events")] public EventReference groupRestingEvent;
        [FoldoutGroup("Group Audio Events")] public EventReference groupTakeOffEvent;
        [FoldoutGroup("Single Audio Events")] public EventReference restingSoundEvent;
        [FoldoutGroup("Single Audio Events")] public EventReference flyingSoundEvent;
        [FoldoutGroup("Single Audio Events")] public EventReference takeOffEvent;
        [FoldoutGroup("Single Audio Events")] public EventReference landEvent;

        [Header("Spawning")]
        [SerializeField] PrefabAndToSpawnCount[] prefabsToSpawn = Array.Empty<PrefabAndToSpawnCount>();

        [SerializeField] FlockRestSpot[] restSpotsToSpawnOn = Array.Empty<FlockRestSpot>();

        [Header("Testing")]
        [Tooltip("If true, all the flock group entities will move towards this gameObject position")]
        public bool overrideTargetPositionToThis;

        public AvoidanceColliderData AvoidanceColliderData { get; private set; }
        public MovementParams MovementParams { get; private set; }
        public MovementStaticParams MovementStaticParams { get; private set; }
        public FlockSoundsData SoundsData { get; private set; }
        public uint FlockHash { get; private set; }
        public float3 InitialPosition { get; private set; }

        public Entity FlockGroupEntity {
            get {
                if (_flockGroupEntity.Equals(default)) {
                    _flockGroupEntity = CreateFlockGroupEntity(World.DefaultGameObjectInjectionWorld.EntityManager);
                }

                return _flockGroupEntity;
            }
        }

        Entity _flockGroupEntity;
        UnsafeList<Entity> _allSpawnedVisualEntities;
        UnsafeList<Entity> _allSpawnedFlockEntities;

        void Awake() {
            FlockHash = math.hash(new int2(this.GetHashCode(), this.GetHashCode()));
            var random = new Unity.Mathematics.Random(FlockHash);
            InitialPosition = random.NextFloat3(transform.position - areaExtents, transform.position + areaExtents);
            var flockData = GetFlockData();

            MovementParams = new MovementParams(movementSpeedMinMax.y, steeringSpeedMultiplierMinMax.x);
            MovementStaticParams = GetMovementStaticParams();
            AvoidanceColliderData = GetAvoidanceColliderData();
            SoundsData = new FlockSoundsData(flyingOrRestingSoundPlayDelayMinMax, groupFlyingEvent.Guid, groupRestingEvent.Guid, groupTakeOffEvent.Guid,
                restingSoundEvent.Guid, flyingSoundEvent.Guid, takeOffEvent.Guid, landEvent.Guid);
            var flockGroupEntity = FlockGroupEntity;
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.SetComponentData(flockGroupEntity, flockData);
            entityManager.SetComponentData(flockGroupEntity, new FlockGroupTargetPosition(InitialPosition));
            entityManager.SetComponentData(flockGroupEntity, new RequestChangeFlockGroupTarget(true));
            // Force update flock group target position
            entityManager.SetComponentData(flockGroupEntity, new FlockGroupLastTargetUpdatedTime(-999));

            CreateListsForSpawnedEntities(out _allSpawnedVisualEntities, out _allSpawnedFlockEntities, ARAlloc.Persistent);
            int usedRestSpotsCount = 0;
            for (int i = 0; i < prefabsToSpawn.Length; i++) {
                var prefabToSpawn = prefabsToSpawn[i];
                var availableRestSpotsToSpawnOn = usedRestSpotsCount < restSpotsToSpawnOn.Length ?
                    new ReadOnlySpan<FlockRestSpot>(restSpotsToSpawnOn, usedRestSpotsCount, restSpotsToSpawnOn.Length - usedRestSpotsCount) :
                    ReadOnlySpan<FlockRestSpot>.Empty;
                usedRestSpotsCount += prefabToSpawn.toSpawnCount;
                SpawnFlockEntities(entityManager, ref _allSpawnedVisualEntities, ref _allSpawnedFlockEntities, prefabToSpawn.flockEntityPrefab, prefabToSpawn.toSpawnCount,
                    availableRestSpotsToSpawnOn);
            }
        }

#if UNITY_EDITOR
        void Update() {
            if (overrideTargetPositionToThis) {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<TargetParams>(), ComponentType.ReadOnly<FlockGroupEntity>());
                var flockGroupEntity = FlockGroupEntity;
                entityQuery.SetSharedComponentFilter(new FlockGroupEntity(flockGroupEntity));
                var flockGroupEntities = entityQuery.ToEntityArray(ARAlloc.Temp);
                float3 newFlockTargetPosition = transform.position;
                for (int i = 0; i < flockGroupEntities.Length; i++) {
                    entityManager.SetComponentData(flockGroupEntities[i], new TargetParams() {
                        flockTargetPosition = newFlockTargetPosition,
                        overridenTargetPosition = newFlockTargetPosition,
                        targetPositionIsRestPosition = false,
                        useOverridenTargetPosition = false,
                    });
                }

                flockGroupEntities.Dispose();
                entityManager.SetComponentData(flockGroupEntity, new FlockGroupTargetPosition(newFlockTargetPosition));
            }
        }
#endif

        void OnDestroy() {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.DestroyEntity(_flockGroupEntity);
            if (_allSpawnedFlockEntities.IsCreated) {
                entityManager.DestroyEntity(_allSpawnedFlockEntities.AsNativeArray());
                _allSpawnedFlockEntities.Dispose();
            }

            if (_allSpawnedVisualEntities.IsCreated) {
                entityManager.DestroyEntity(_allSpawnedVisualEntities.AsNativeArray());
                _allSpawnedVisualEntities.Dispose();
            }
        }

        [Button]
        public static void ScareAllEntitiesFromRestPositions() {
            FlockRestSpotSystem.ReleaseAllEntitiesInRestSpots();
        }

        [Button]
        public static void ForceActivateAllRestSpots() {
            FlockRestSpotSystem.ForceActivateAllRestSpots(5f);
        }

        [Button]
        public static void StopForceActivateAllRestSpots() {
            FlockRestSpotSystem.StopForceActiveAllRestSpots();
        }

        static void OverrideLastLodDistance(ref float4 lodDistances0, ref float4 lodDistances1, float maxLodNewDistance) {
            try {
                var infinityLodIndexInDistances0 = mathExt.IndexOf(float.PositiveInfinity, lodDistances0);
                if (infinityLodIndexInDistances0 == 0) {
                    return;
                }
                if (infinityLodIndexInDistances0 != -1) {
                    if (infinityLodIndexInDistances0 >= 2) {
                        maxLodNewDistance = math.max(lodDistances0[infinityLodIndexInDistances0 - 2], maxLodNewDistance);
                    }
                    lodDistances0[infinityLodIndexInDistances0 - 1] = maxLodNewDistance;
                    return;
                }
                var infinityLodIndexInDistances1 = mathExt.IndexOf(float.PositiveInfinity, lodDistances1);
                if (infinityLodIndexInDistances1 == 0) {
                    lodDistances0[3] = math.max(lodDistances0[2], maxLodNewDistance);
                    return;
                }
                if (infinityLodIndexInDistances1 == 1) {
                    lodDistances1[0] = math.max(lodDistances0[3], maxLodNewDistance);
                    return;
                }
                if (infinityLodIndexInDistances1 != -1) {
                    lodDistances1[infinityLodIndexInDistances1 - 1] = math.max(lodDistances1[infinityLodIndexInDistances1 - 2], maxLodNewDistance);
                    return;
                }
                lodDistances1[3] = math.max(lodDistances1[2], maxLodNewDistance);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
        
        void SpawnFlockEntities(EntityManager entityManager, ref UnsafeList<Entity> allSpawnedVisualEntities, ref UnsafeList<Entity> allSpawnedFlockEntities,
            GameObject flockEntityPrefab, int toSpawnCount, ReadOnlySpan<FlockRestSpot> restSpotsToSpawnOn) {
            if (flockEntityPrefab == null) {
                Log.Important?.Error($"{nameof(flockEntityPrefab)} is not assigned to {nameof(FlockGroup)} {name} on {gameObject.PathInSceneHierarchy()}", gameObject);
                return;
            }

            var options = new IWithUnityRepresentation.Options {
                linkedLifetime = false,
                movable = false
            };

            var prefabInstanceDrakeLodGroup = DrakeRuntimeSpawning.InstantiatePrefab(flockEntityPrefab, options);
            if (prefabInstanceDrakeLodGroup == null) {
                Log.Important?.Error($"{nameof(flockEntityPrefab)} {flockEntityPrefab.name} does not have {nameof(DrakeLodGroup)} component on {gameObject.PathInSceneHierarchy()}", gameObject);
                return;
            }
            if (overrideEntitiesMaxRenderDistance) {
                LodGroupSerializableData lodGroupData = prefabInstanceDrakeLodGroup.LodGroupSerializableDataRaw;
                OverrideLastLodDistance(ref lodGroupData.lodDistances0, ref lodGroupData.lodDistances1, entitiesMaxRenderDistanceOverride);
                prefabInstanceDrakeLodGroup.LodGroupSerializableDataRaw = lodGroupData;
                var drakeMeshRenderers = prefabInstanceDrakeLodGroup.Renderers;
                foreach (var drakeMeshRenderer in drakeMeshRenderers) {
                    drakeMeshRenderer.PrepareRanges(lodGroupData.lodDistances0, lodGroupData.lodDistances1);
                }
            }
            var prefabInstanceFlockEntityComponent = prefabInstanceDrakeLodGroup.GetComponent<FlockEntity>();
            if (prefabInstanceFlockEntityComponent == null) {
                Log.Important?.Error($"{nameof(flockEntityPrefab)} {flockEntityPrefab.name} does not have {nameof(FlockEntity)} component on {gameObject.PathInSceneHierarchy()}", gameObject);
                return;
            }

            // Assign scale so that when baking drake lod bounds would be properly scaled
            prefabInstanceFlockEntityComponent.SetRandomScale(flockEntityMinMaxScale);
            var prefabInstance = prefabInstanceDrakeLodGroup.gameObject;


            var tempDataEntity = entityManager.CreateEntity(DrakeRuntimeSpawning.DataEntityComponentTypes);
            DrakeRuntimeSpawning.CreateAndAddDrakeEntityPrefabs(prefabInstanceDrakeLodGroup, gameObject.scene, tempDataEntity, entityManager, ARAlloc.Temp,
                out var prefabEntities);

            entityManager.DestroyEntity(prefabInstanceFlockEntityComponent.Entity);
            Object.Destroy(prefabInstance);

            var prefabsDatas = entityManager.GetBuffer<DrakeStaticPrefabData>(tempDataEntity).AsNativeArray().CreateCopy(ARAlloc.Temp);
            entityManager.DestroyEntity(tempDataEntity);

            var random = new Unity.Mathematics.Random(FlockHash);
            var center = transform.position;
            float3 randomPosMin = center - positionVarianceExtent;
            float3 randomPosMax = center + positionVarianceExtent;
            int drakeEntitiesPerInstanceCount = prefabEntities.Length;

            var allSpawnedVisualEntitiesInitialLength = allSpawnedVisualEntities.Length;
            allSpawnedVisualEntities.Resize(allSpawnedVisualEntitiesInitialLength + (toSpawnCount * drakeEntitiesPerInstanceCount));
            allSpawnedFlockEntities.Resize(allSpawnedFlockEntities.Length + toSpawnCount);
            int usedRestSpotsCount = 0;
            // component types without LinkedLifetimeRequest
            var flockEntityComponentTypes = new ReadOnlySpan<ComponentType>(FlockEntity.FlockEntityComponentTypes, 0, FlockEntity.FlockEntityComponentTypes.Length - 1);
            var flockEntityArchetype = entityManager.CreateArchetype(flockEntityComponentTypes);
            for (int i = 0; i < toSpawnCount; i++) {
                float3 spawnPosition = usedRestSpotsCount < restSpotsToSpawnOn.Length ?
                    restSpotsToSpawnOn[usedRestSpotsCount].transform.position :
                    random.NextFloat3(randomPosMin, randomPosMax);
                usedRestSpotsCount++;
                quaternion randomRotation = random.NextQuaternionRotation();
                var randomScale = random.NextFloat(flockEntityMinMaxScale.x, flockEntityMinMaxScale.y);
                var flockEntity = entityManager.CreateEntity(flockEntityArchetype);
                allSpawnedFlockEntities[i] = flockEntity;
                FlockEntity.SetupFromFlockGroup(flockEntity, new DrakeVisualEntitiesTransform(spawnPosition, randomRotation, randomScale), this, entityManager);
                var spawnedVisualEntities = allSpawnedVisualEntities.AsNativeArray().GetSubArray(
                    allSpawnedVisualEntitiesInitialLength + (i * drakeEntitiesPerInstanceCount), drakeEntitiesPerInstanceCount);
                DrakeRuntimeSpawning.SpawnDrakeEntities(in prefabEntities, in prefabsDatas, in spawnPosition, in randomRotation, randomScale, ref entityManager, ref spawnedVisualEntities);
                var visualEntitiesBuffer = entityManager.GetBuffer<DrakeVisualEntity>(flockEntity).Reinterpret<Entity>();
                // also resizes buffer it to length of spawnedVisualEntities
                visualEntitiesBuffer.CopyFrom(spawnedVisualEntities);
            }

            entityManager.DestroyEntity(prefabEntities);
            prefabEntities.Dispose();
            prefabsDatas.Dispose();
        }

        void CreateListsForSpawnedEntities(out UnsafeList<Entity> allSpawnedVisualEntities, out UnsafeList<Entity> allSpawnedFlockEntities, Allocator allocator) {
            int allEntitiesToSpawnCount = 0;
            for (int i = 0; i < prefabsToSpawn.Length; i++) {
                allEntitiesToSpawnCount += prefabsToSpawn[i].flockEntityPrefab != null ? prefabsToSpawn[i].toSpawnCount : 0;
            }

            const int CommonDrakeLODLevelsCount = 3;
            allSpawnedVisualEntities = new UnsafeList<Entity>(allEntitiesToSpawnCount * CommonDrakeLODLevelsCount, allocator);
            allSpawnedFlockEntities = new UnsafeList<Entity>(allEntitiesToSpawnCount, allocator);
        }

        FlockData GetFlockData() {
            var maxRenderDistance = GetFlockEntitiesMaxRenderDistance();
            var flockGroupSimulationDistance = math.max(maxRenderDistance, minimalSimulateDistance);
            return new FlockData(FlockHash, transform.position, areaExtents, positionVarianceExtent, diveOnReachedTargetChance,
                diveMaxHeightDiff, maxDelayForUsingNewFlockTarget, steeringSpeedMultiplierMinMax, movementSpeedMinMax, targetPositionUpdateMinMax, flockGroupSimulationDistance);
        }

        float GetFlockEntitiesMaxRenderDistance() {
            if (overrideEntitiesMaxRenderDistance) {
                return entitiesMaxRenderDistanceOverride;
            }
            float maxDistance = 0;
            for (int i = 0; i < prefabsToSpawn.Length; i++) {
                var prefab = prefabsToSpawn[i].flockEntityPrefab;
                if (prefab == null) {
                    continue;
                }

                if (prefab.TryGetComponent(out DrakeLodGroup drakeLodGroup) == false) {
                    continue;
                }

                maxDistance = math.max(drakeLodGroup.LodGroupSerializableData.GetMaxRenderingDistance(), maxDistance);
            }

            return maxDistance;
        }

        MovementStaticParams GetMovementStaticParams() {
            return new MovementStaticParams(movementAcceleration, movementDeceleration, maxDecelerationForReachRestPosition, minSpeedForMovingToRestPosition,
                toRestSteeringSpeedMult, avoidanceSteeringParams, avoidanceSpeedMultiplierCurvePow, avoidanceRotationSpeedAdditionWhenExceeding);
        }

        AvoidanceColliderData GetAvoidanceColliderData() {
            var toRightVector = math.rotate(quaternion.RotateY(math.radians(forwardCheckRayAngle)), math.forward()) * forwardCheckRayLength;
            float vectorLengthOnRightAxis = math.dot(math.right(), toRightVector);
            float vectorLengthOnForwardAxis = math.dot(math.forward(), toRightVector);
            return new AvoidanceColliderData(avoidanceLayerMask, colliderRadius, vectorLengthOnRightAxis, vectorLengthOnForwardAxis);
        }

        Entity CreateFlockGroupEntity(EntityManager entityManager) {
            var entity = entityManager.CreateEntity(
                ComponentType.ReadWrite<FlockData>(), ComponentType.ReadWrite<RequestChangeFlockGroupTarget>(),
                ComponentType.ReadWrite<FlockGroupLastTargetUpdatedTime>(), ComponentType.ReadWrite<FlockGroupTargetPosition>());
            entityManager.SetName(entity, $"FlockGroup_{name}");
            return entity;
        }

        void UpdateParams() {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<MovementStaticParams>(), ComponentType.ReadWrite<AvoidanceColliderData>(),
                ComponentType.ReadWrite<FlyingFlockEntityAnimationsData>(), ComponentType.ReadOnly<FlockGroupEntity>());
            var flockGroupEntity = FlockGroupEntity;
            entityQuery.SetSharedComponentFilter(new FlockGroupEntity(flockGroupEntity));
            var flockGroupEntities = entityQuery.ToEntityArray(ARAlloc.Temp);
            var newestMovementStaticParams = GetMovementStaticParams();
            var newestAvoidanceColliderData = GetAvoidanceColliderData();
            var newestAnimationsData = animationsData;
            for (int i = 0; i < flockGroupEntities.Length; i++) {
                var entity = flockGroupEntities[i];
                entityManager.SetComponentData(entity, newestMovementStaticParams);
                entityManager.SetComponentData(entity, newestAvoidanceColliderData);
                entityManager.SetComponentData(entity, newestAnimationsData);
            }

            entityManager.SetComponentData(flockGroupEntity, GetFlockData());
            flockGroupEntities.Dispose();
        }

#if UNITY_EDITOR
        void OnValidate() {
            if (Application.isPlaying) {
                UpdateParams();
            }
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.red;
            var avoidanceColliderData = GetAvoidanceColliderData();
            var transformPosition = transform.position;
            Gizmos.DrawRay(transformPosition, (transform.forward * avoidanceColliderData.vectorLenghtOnForwardAxis) + (transform.right * avoidanceColliderData.vectorLenghtOnRightAxis));
            Gizmos.DrawRay(transformPosition, (transform.forward * avoidanceColliderData.vectorLenghtOnForwardAxis) - (transform.right * avoidanceColliderData.vectorLenghtOnRightAxis));
            Gizmos.DrawRay(transformPosition, transform.up * colliderRadius);
            Gizmos.DrawRay(transformPosition, transform.up * -colliderRadius);

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transformPosition, areaExtents * 2);
            
            var maxRenderDistance = GetFlockEntitiesMaxRenderDistance();
            var flockGroupSimulationDistance = math.max(maxRenderDistance, minimalSimulateDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transformPosition, flockGroupSimulationDistance);
            
            if (Application.isPlaying) {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TargetParams>(), ComponentType.ReadOnly<FlockGroupEntity>());
                query.SetSharedComponentFilter(new FlockGroupEntity() {
                    value = FlockGroupEntity
                });
                var currentTime = Time.time;
                var targetParamsArr = query.ToComponentDataArray<TargetParams>(ARAlloc.Temp);
                for (int i = 0; i < targetParamsArr.Length; i++) {
                    var targetParams = targetParamsArr[i];
                    bool useOverridenTargetPosition = targetParams.useOverridenTargetPosition | (targetParams.useFlockTargetPosMinTime > currentTime);
                    Gizmos.color = useOverridenTargetPosition ? Color.cyan : Color.green;
                    var targetPos = math.select(targetParams.flockTargetPosition, targetParams.overridenTargetPosition, useOverridenTargetPosition);
                    Gizmos.DrawWireCube(targetPos, new float3(0.1f));
                }

                targetParamsArr.Dispose();
            } else {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transformPosition, new float3(positionVarianceExtent * 2));
            }

            Gizmos.color = Color.white;
        }
#endif
        [Serializable]
        struct PrefabAndToSpawnCount {
            public GameObject flockEntityPrefab;
            [Range(0, MaxEntitiesCount)] public int toSpawnCount;
        }
    }
}