using System;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Awaken.ECS.Elevator {
    [UpdateInGroup(typeof(ElevatorSystemGroup))]
    [UpdateAfter(typeof(UpdateElevatorPlatformPositionSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class MoveElevatorChainsSystem : SystemBase {
        EntityQuery _elevatorChainDataQuery;
        int _prevElevatorsCount = 4;
        protected override void OnCreate() {
            _elevatorChainDataQuery = GetEntityQuery(ComponentType.ReadOnly<ElevatorChainData>());
        }

        protected override void OnUpdate() {
            var elevatorChainDatas = _elevatorChainDataQuery.ToComponentDataArray<ElevatorChainData>(ARAlloc.Temp);

            var yOffsetWithEntityCounts = new UnsafeList<YOffsetWithEntityArray>(_prevElevatorsCount, ARAlloc.TempJob);
            int count = elevatorChainDatas.Length;
            
            for (int i = 0; i < count; i++) {
                var elevatorChainData = elevatorChainDatas[i];
                var yOffset = elevatorChainData.platformCurrentPositionY - elevatorChainData.platformPrevPositionY;
                if (yOffset == 0) {
                    continue;
                }
                var spawnedEntities = elevatorChainData.spawnedEntities.AsArray();
                unsafe {
                    yOffsetWithEntityCounts.Add(new YOffsetWithEntityArray(yOffset, spawnedEntities.Length, (IntPtr)spawnedEntities.GetUnsafeReadOnlyPtr()));
                }
            }
            
            if (yOffsetWithEntityCounts.Length != 0) {
                _prevElevatorsCount = yOffsetWithEntityCounts.Length;
                
                Dependency = new MoveChainsJob() {
                    yOffsetsWithEntityCounts = yOffsetWithEntityCounts, // Deallocated on job completion
                    localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(),
                    wordRenderBoundsLookup = SystemAPI.GetComponentLookup<WorldRenderBounds>(),
                    lodWorldReferencePointLookup = SystemAPI.GetComponentLookup<LODWorldReferencePoint>(),
                }.Schedule(Dependency);
            } else {
                _prevElevatorsCount = 1;
                yOffsetWithEntityCounts.Dispose();
            }
            
            elevatorChainDatas.Dispose();
        }
    }
    
    [BurstCompile]
    public unsafe struct MoveChainsJob : IJob {
        [ReadOnly, DeallocateOnJobCompletion] public UnsafeList<YOffsetWithEntityArray> yOffsetsWithEntityCounts;
        public ComponentLookup<LocalToWorld> localToWorldLookup;
        public ComponentLookup<WorldRenderBounds> wordRenderBoundsLookup;
        public ComponentLookup<LODWorldReferencePoint> lodWorldReferencePointLookup;

        public void Execute() {
            int groupsCount = yOffsetsWithEntityCounts.Length;
            for (int groupIndex = 0; groupIndex < groupsCount; groupIndex++) {
                var (yOffset, entityCount, entityArrayPtr) = yOffsetsWithEntityCounts[groupIndex];
                var chainPartsEntities = (Entity*)entityArrayPtr;
                if (chainPartsEntities == null) {
                    Debug.LogError($"{nameof(chainPartsEntities)} ptr is null");
                    continue;
                }
                for (int entityIndex = 0; entityIndex < entityCount; entityIndex++) {
                    var entity = chainPartsEntities[entityIndex];
                    var localToWorld = localToWorldLookup[entity];
                    localToWorld.Value.c3.y += yOffset;
                    localToWorldLookup[entity] = localToWorld;

                    var worldRenderBounds = wordRenderBoundsLookup[entity];
                    worldRenderBounds.Value.Center.y += yOffset;
                    wordRenderBoundsLookup[entity] = worldRenderBounds;

                    var lodWorldReferencePoint = lodWorldReferencePointLookup[entity];
                    lodWorldReferencePoint.Value.y += yOffset;
                    lodWorldReferencePointLookup[entity] = lodWorldReferencePoint;
                }
                
            }
        }
    }
    
    public struct YOffsetWithEntityArray {
        public float yOffset;
        public int entityCount;
        public IntPtr entityArrayPtr;

        public YOffsetWithEntityArray(float yOffset, int entityCount, IntPtr entityArrayPtr) {
            this.yOffset = yOffset;
            this.entityCount = entityCount;
            this.entityArrayPtr = entityArrayPtr;
        }

        public void Deconstruct(out float yOffset, out int entityCount, out IntPtr entityArrayPtr) {
            yOffset = this.yOffset;
            entityCount = this.entityCount;
            entityArrayPtr = this.entityArrayPtr;
        }
    } 
}