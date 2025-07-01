using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.Flocks {
    [UpdateInGroup(typeof(FlockSystemGroup))]
    [BurstCompile] [RequireMatchingQueriesForUpdate]
    public partial class FlockEntityAvoidanceDataSetSystem : SystemBase {
        const int RaysPerEntityCount = 4;
        const int RaycastEntitiesPreallocateCount = 64;
        const int RaycastCommandsPreallocateCount = RaycastEntitiesPreallocateCount * RaysPerEntityCount;
        const int MinRaycastCommandsPerJob = 32;
        const int MaxHitsPerCommand = 1;
        EntityQuery _avoidanceEntitiesQuery;
        NativeArray<RaycastCommand> _raycastCommandsBuff;
        NativeArray<RaycastHit> _raycastHits;
        NativeArray<Entity> _raycastEntities;
        ComponentLookup<AvoidanceData> _avoidanceDataLookup;

        int prevRaycastEntitiesCount;

        protected override void OnCreate() {
            base.OnCreate();
            _raycastCommandsBuff = new NativeArray<RaycastCommand>(RaycastCommandsPreallocateCount, ARAlloc.Persistent);
            _raycastEntities = new NativeArray<Entity>(RaycastEntitiesPreallocateCount, ARAlloc.Persistent);
            _raycastHits = new NativeArray<RaycastHit>(RaycastCommandsPreallocateCount, ARAlloc.Persistent);

            _avoidanceEntitiesQuery = SystemAPI.QueryBuilder()
                .WithPresent<AvoidanceColliderData, DrakeVisualEntitiesTransform, AvoidanceData>()
                .WithNone<CulledEntityTag>().Build();
            _avoidanceDataLookup = SystemAPI.GetComponentLookup<AvoidanceData>();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            Dependency.Complete();
            _raycastCommandsBuff.Dispose();
            _raycastEntities.Dispose();
            _raycastHits.Dispose();
        }

        protected override void OnUpdate() {
            Dependency.Complete();
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime == 0) {
                return;
            }

            _avoidanceDataLookup.Update(this);
            var entitiesCount = _avoidanceEntitiesQuery.CalculateEntityCount();
            var raycastsCount = entitiesCount * RaysPerEntityCount;
            if (_raycastEntities.Length < entitiesCount) {
                _raycastEntities.Resize(entitiesCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                _raycastHits.Resize(raycastsCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
                _raycastCommandsBuff.Resize(raycastsCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            var currentFrame = UnityEngine.Time.frameCount;
            Dependency = new SetRaycastDataJob() {
                entities = _raycastEntities,
                hits = _raycastHits,
                avoidanceDataLookup = _avoidanceDataLookup,
                valueSetFrame = currentFrame,
                entitiesCount = prevRaycastEntitiesCount
            }.Schedule(Dependency);

            prevRaycastEntitiesCount = entitiesCount;

            var raycastCommands = _raycastCommandsBuff.GetSubArray(0, raycastsCount);
            var raycastEntities = _raycastEntities.GetSubArray(0, entitiesCount);
            var raycastHits = _raycastHits.GetSubArray(0, raycastsCount);

            var entityIndex = 0;
            Dependency = Entities.WithAll<AvoidanceData>().WithNone<CulledEntityTag>()
                .ForEach((Entity entity, in AvoidanceColliderData avoidanceColliderData, in DrakeVisualEntitiesTransform flockEntityTransform) => {
                    var position = (Vector3)flockEntityTransform.position;
                    raycastEntities[entityIndex] = entity;
                    var entityRaycastCommands = raycastCommands.GetSubArray(entityIndex * RaysPerEntityCount, RaysPerEntityCount);
                    entityIndex++;
                    var radius = avoidanceColliderData.radius;
                    var maskValue = avoidanceColliderData.mask.value;
                    var forwardDir = flockEntityTransform.Forward;
                    var rightDir = flockEntityTransform.Right;
                    var queryParams = new QueryParameters(maskValue, false, QueryTriggerInteraction.Ignore, false);
                    entityRaycastCommands[0] = new RaycastCommand(position, Vector3.up, queryParams, radius);
                    entityRaycastCommands[1] = new RaycastCommand(position, Vector3.down, queryParams, radius);
                    var vectorLenghtOnRightAxis = rightDir * avoidanceColliderData.vectorLenghtOnRightAxis;
                    var vectorLenghtOnForwardAxis = avoidanceColliderData.vectorLenghtOnForwardAxis;
                    var forwardVector = forwardDir * vectorLenghtOnForwardAxis;
                    var toRightVector = rightDir * vectorLenghtOnRightAxis;

                    var leftCheckVector = forwardVector - toRightVector;
                    var rightCheckVector = forwardVector + toRightVector;
                    var forwardCheckDistance = math.length(leftCheckVector);
                    var rightCheckDir = rightCheckVector / forwardCheckDistance;
                    var leftCheckDir = leftCheckVector / forwardCheckDistance;
                    entityRaycastCommands[2] = new RaycastCommand(position, rightCheckDir, queryParams, forwardCheckDistance);
                    entityRaycastCommands[3] = new RaycastCommand(position, leftCheckDir, queryParams, forwardCheckDistance);
#if UNITY_EDITOR && ENABLE_FLOCKS_DEBUG_RAYS
                    Debug.DrawRay(position, rightCheckDir * forwardCheckDistance);
                    Debug.DrawRay(position, leftCheckDir * forwardCheckDistance);
                    Debug.DrawRay(position, Vector3.up * radius);
                    Debug.DrawRay(position, Vector3.down * radius);
#endif
                }).Schedule(Dependency);

            Dependency = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, MinRaycastCommandsPerJob, MaxHitsPerCommand, Dependency);
        }

        [BurstCompile]
        struct SetRaycastDataJob : IJob {
            [ReadOnly] public NativeArray<Entity> entities;
            public NativeArray<RaycastHit> hits;
            public ComponentLookup<AvoidanceData> avoidanceDataLookup;
            public int entitiesCount;
            public int valueSetFrame;

            [BurstCompile]
            public void Execute() {
                int count = entitiesCount;
                for (int i = 0; i < count; i++) {
                    var entity = entities[i];
                    var entityHits = hits.GetSubArray(i * RaysPerEntityCount, RaysPerEntityCount);
                    var avoidanceDataRef = avoidanceDataLookup.GetRefRWOptional(entity);
                    if (Hint.Unlikely(avoidanceDataRef.IsValid == false)) {
                        continue;
                    }

                    var upHit = entityHits[0];
                    var downHit = entityHits[1];
                    var rightHit = entityHits[2];
                    var leftHit = entityHits[3];

                    var isUpHit = upHit.normal != default;
                    var isDownHit = downHit.normal != default;
                    var isRightHit = rightHit.normal != default;
                    var isLeftHit = leftHit.normal != default;

                    bool isAnyHit = math.any(new bool4(isUpHit, isDownHit, isRightHit, isLeftHit));
                    if (Hint.Likely(isAnyHit == false)) {
                        continue;
                    }

                    bool isUpDownHit = isUpHit | isDownHit;
                    var upDownHitPoint = math.select(downHit.point, upHit.point, isUpDownHit & isUpHit);

                    bool isRightLeftHit = isRightHit | isLeftHit;
                    var rightHitPoint = math.select(upDownHitPoint, rightHit.point, isRightHit);
                    var rightLeftHitPoint = math.select(rightHitPoint, leftHit.point, isRightLeftHit & !isRightHit);

                    upDownHitPoint = math.select(rightLeftHitPoint, upDownHitPoint, isUpDownHit);

                    avoidanceDataRef.ValueRW = new AvoidanceData(upDownHitPoint, rightLeftHitPoint, valueSetFrame);
                }
            }
        }
    }
}