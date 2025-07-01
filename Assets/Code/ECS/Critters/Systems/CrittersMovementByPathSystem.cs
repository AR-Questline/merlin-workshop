using System;
using Awaken.ECS.Components;
using Awaken.ECS.Critters.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Awaken.ECS.Critters {
    [UpdateInGroup(typeof(CrittersSystemGroup))]
    [BurstCompile, RequireMatchingQueriesForUpdate]
    public unsafe partial class CrittersMovementByPathSystem : SystemBase {
        const float ArriveAtPointDistanceThreshold = 0.02f;   
        const float ArriveAtPointDistanceSqThreshold = ArriveAtPointDistanceThreshold * ArriveAtPointDistanceThreshold;   
        const float SteepSlopeTopDownPointsDistanceThresholdSq = 0.2f * 0.2f;
        const float RotationAngleAlignEpsilon = 2 * math.TORADIANS;

        EntityQuery _query;
        EntityTypeHandle _entityTypeHandle;
        ComponentTypeHandle<CritterIndexInGroup> _indexInGroupHandle;
        ComponentTypeHandle<DrakeVisualEntitiesTransform> _drivingTransformHandle;
        ComponentTypeHandle<CritterMovementState> _movementStateHandle;
        ComponentTypeHandle<CritterAnimatorParams> _animatorParamsHandle;
        SharedComponentTypeHandle<CritterGroupSharedData> _crittersSharedDataHandle;

        protected override void OnCreate() {
            _query = SystemAPI.QueryBuilder().WithAllRW<DrakeVisualEntitiesTransform, CritterMovementState>()
                .WithAllRW<CritterAnimatorParams>().WithAll<CritterGroupSharedData>().WithNone<CulledEntityTag>().Build();

            _entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            _indexInGroupHandle = SystemAPI.GetComponentTypeHandle<CritterIndexInGroup>(true);
            _crittersSharedDataHandle = SystemAPI.GetSharedComponentTypeHandle<CritterGroupSharedData>();
            _drivingTransformHandle = SystemAPI.GetComponentTypeHandle<DrakeVisualEntitiesTransform>();
            _movementStateHandle = SystemAPI.GetComponentTypeHandle<CritterMovementState>();
            _animatorParamsHandle = SystemAPI.GetComponentTypeHandle<CritterAnimatorParams>();
        }

        protected override void OnUpdate() {
            var elapsedTimeHash = (int)math.hash(new double2(SystemAPI.Time.ElapsedTime));
            var deltaTime = SystemAPI.Time.DeltaTime;

            ref var state = ref CheckedStateRef;
            _entityTypeHandle.Update(ref state);
            _indexInGroupHandle.Update(ref state);
            _crittersSharedDataHandle.Update(ref state);
            _drivingTransformHandle.Update(ref state);
            _movementStateHandle.Update(ref state);
            _animatorParamsHandle.Update(ref state);
            Dependency = new MoveCrittersAlongPathsJob() {
                entityTypeHandle = _entityTypeHandle,
                crittersSharedDataHandle = _crittersSharedDataHandle,
                indexInGroupHandle = _indexInGroupHandle,
                drivingTransformHandle = _drivingTransformHandle,
                movementStateHandle = _movementStateHandle,
                animatorParamsHandle = _animatorParamsHandle,
                elapsedTimeHash = elapsedTimeHash,
                deltaTime = deltaTime
            }.Schedule(_query, Dependency);
        }

        [BurstCompile]
        struct MoveCrittersAlongPathsJob : IJobChunk {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<CritterIndexInGroup> indexInGroupHandle;
            [ReadOnly] public SharedComponentTypeHandle<CritterGroupSharedData> crittersSharedDataHandle;
            public ComponentTypeHandle<DrakeVisualEntitiesTransform> drivingTransformHandle;
            public ComponentTypeHandle<CritterMovementState> movementStateHandle;
            public ComponentTypeHandle<CritterAnimatorParams> animatorParamsHandle;

            public int elapsedTimeHash;
            public float deltaTime;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var sharedData = chunk.GetSharedComponent(crittersSharedDataHandle);
                var entities = chunk.GetEntityDataPtrRO(entityTypeHandle);
                var indicesInGroup = chunk.GetComponentDataPtrRO(ref indexInGroupHandle);
                var transforms = chunk.GetComponentDataPtrRW(ref drivingTransformHandle);
                var movementStates = chunk.GetComponentDataPtrRW(ref movementStateHandle);
                var animatorsParams = chunk.GetComponentDataPtrRW(ref animatorParamsHandle);
                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var entity = entities[i];
                    ref var transform = ref transforms[i];
                    ref var walkState = ref movementStates[i];
                    ref var animatorParams = ref animatorsParams[i];

                    ref var allPathPointsRef = ref sharedData.pathPointsRef.Value.pathPointsData;
                    ref var pathsRanges = ref sharedData.pathPointsRef.Value.pathsRanges;
                    
                    walkState.PrevIsMoving = walkState.idleWaitTimeRemaining <= 0;

                    var pathIndex = indicesInGroup[i].value;
                    var (pathRangeStart, pathRangeLenght) = pathsRanges[pathIndex];

                    var pathPoints = NativeCollectionsExt.ConvertExistingDataToNativeArray(
                        ((float3 position, uint spheremapCompressedNormal)*)allPathPointsRef.GetUnsafePtr() + pathRangeStart, pathRangeLenght);

                    int pathPointsCount = pathPoints.Length;
                    
                    int fromPathPointIndex, toPathPointIndex;
                    (float3 position, uint spheremapCompressedNormal) fromPathPoint, toPathPoint;
                    if (Hint.Unlikely(walkState.directionToNextPoint.Equals(default))) {
                        fromPathPointIndex = pathPointsCount - 1;
                        toPathPointIndex = 0;
                        fromPathPoint = pathPoints[fromPathPointIndex];
                        toPathPoint = pathPoints[toPathPointIndex];
                        var fromToVector = toPathPoint.position - fromPathPoint.position;
                        walkState.currentPathSegmentLength = math.length(fromToVector);
                        walkState.directionToNextPoint = fromToVector / walkState.currentPathSegmentLength;
                        transform.position = toPathPoint.position;
                        // this will ensure that branch with "reachedDestinationPoint == true" will be taken
                    } else {
                        fromPathPointIndex = walkState.currentPathPointIndex;
                        toPathPointIndex = (walkState.currentPathPointIndex + 1) % pathPointsCount;
                        fromPathPoint = pathPoints[fromPathPointIndex];
                        toPathPoint = pathPoints[toPathPointIndex];
                    }
                    
                    var random = new Random(math.hash(new int3(entity.Index, entity.Version, elapsedTimeHash)));
                    var distanceAlongCurrentSegment = math.dot(walkState.directionToNextPoint, transform.position - fromPathPoint.position);
                    bool reachedDestinationPoint = distanceAlongCurrentSegment > walkState.currentPathSegmentLength - ArriveAtPointDistanceThreshold;
                    if (Hint.Unlikely(reachedDestinationPoint)) {
                        var nextPathPointIndex = (toPathPointIndex + 1) % pathPointsCount;
                        var nextPathPoint = pathPoints[nextPathPointIndex];
                        // Points are spaces as far as possible from each other, inserting a new point only if height changes significantly enough.
                        // If there is a steep slope, like a straight wall, there will be a point at the bottom and a point at the top of the wall, and
                        // top-down distance between those points will be almost zero. 
                        bool isCurrentlyOnSteepSlope = math.distancesq(fromPathPoint.position.xz, toPathPoint.position.xz) < SteepSlopeTopDownPointsDistanceThresholdSq;
                        bool nextPathSegmentIsSteepSlope = math.distancesq(toPathPoint.position.xz, nextPathPoint.position.xz) < SteepSlopeTopDownPointsDistanceThresholdSq;
                        
                        // If points have high elevation difference - do not stop critter there because it will clip through terrain 
                        bool useIdle = (!isCurrentlyOnSteepSlope && !nextPathSegmentIsSteepSlope) & (random.NextFloat() < sharedData.movementParams.idleChance);
                        walkState.currentPathPointIndex = toPathPointIndex;
                        walkState.idleWaitTimeRemaining = math.select(0, random.NextFloat(sharedData.movementParams.idleTimeMin, sharedData.movementParams.idleTimeMax), useIdle);

                        // Change speed only after idle animation to avoid creating rapid acceleration out of nowhere
                        walkState.movementSpeed = math.select(walkState.movementSpeed, random.NextFloat(
                            sharedData.movementParams.movementSpeedMin, sharedData.movementParams.movementSpeedMax), useIdle);

                        var currentSegmentNormalDir = CompressionUtils.DecodeNormalVectorSpheremap(toPathPoint.spheremapCompressedNormal);

                        var vectorToNextPoint = nextPathPoint.position - toPathPoint.position;
                        walkState.currentPathSegmentLength = math.length(vectorToNextPoint);
                        if (walkState.currentPathSegmentLength.Equals(0)) {
                            throw new Exception($"Current path segment lenght is 0");
                        }
                        walkState.directionToNextPoint = vectorToNextPoint / walkState.currentPathSegmentLength;

                        walkState.alignmentRotationTowardNextPoint = quaternion.LookRotation(walkState.directionToNextPoint, currentSegmentNormalDir);
                    }

                    var rotationSpeedRad = math.radians(sharedData.movementParams.rotationSpeed);
                    transform.rotation = GetSmoothRotationTowards(transform.rotation, walkState.alignmentRotationTowardNextPoint, rotationSpeedRad, deltaTime);
                    var newIdleTimeRemaining = walkState.idleWaitTimeRemaining - deltaTime;
                    bool isMoving = newIdleTimeRemaining <= 0;
                    walkState.IsMoving = isMoving;
                    if (isMoving) {
                        walkState.idleWaitTimeRemaining = 0;
                        transform.position += walkState.directionToNextPoint * walkState.movementSpeed * deltaTime;
                        if (Hint.Unlikely(animatorParams.value.targetAnimationIndex != CritterEntityData.WalkingAnimationIndex)) {
                            animatorParams.value.transitionTime = CritterEntityData.ToAndFromIdleTransitionTime;
                            animatorParams.value.targetAnimationIndex = CritterEntityData.WalkingAnimationIndex;
                            animatorParams.value.targetAnimationSpeed = walkState.movementSpeed;
                        }
                    } else {
                        walkState.idleWaitTimeRemaining = newIdleTimeRemaining;
                        if (Hint.Unlikely(animatorParams.value.targetAnimationIndex != CritterEntityData.IdleAnimationIndex)) {
                            animatorParams.value.transitionTime = CritterEntityData.ToAndFromIdleTransitionTime;
                            animatorParams.value.targetAnimationIndex = CritterEntityData.IdleAnimationIndex;
                            animatorParams.value.targetAnimationSpeed = 1f;
                        }
                    }
                }
            }

            static quaternion GetSmoothRotationTowards(quaternion currentRotation, quaternion targetRotation, float rotationSpeed, float deltaTime) {
                var angleBetweenCurrentAndToTargetRotation = math.angle(currentRotation, targetRotation);
                var rotationSlerpValue = math.select(math.min((rotationSpeed * deltaTime) / angleBetweenCurrentAndToTargetRotation, 1), 1,
                    angleBetweenCurrentAndToTargetRotation < RotationAngleAlignEpsilon);
                var smoothRotationToTarget = math.slerp(currentRotation, targetRotation, rotationSlerpValue);
                return smoothRotationToTarget;
            }
        }
    }
}