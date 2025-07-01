using Awaken.ECS.Components;
using Awaken.ECS.Critters.Components;
using Awaken.ECS.DrakeRenderer;
using Awaken.Utility.Collections;
using FMODUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Awaken.ECS.Critters {
    [UpdateInGroup(typeof(CrittersSystemGroup))]
    [UpdateBefore(typeof(CrittersMovementByPathSystem))]
    [RequireMatchingQueriesForUpdate]
    public unsafe partial class CrittersMovementByPathSoundSystem : SystemBase {
        EntityQuery _query;
        ComponentTypeHandle<CritterMovementState> movementStateHandle;
        SharedComponentTypeHandle<CritterGroupSharedData> groupSharedDataHandle;
        ComponentTypeHandle<StudioEventEmitter> audioEmitterHandle;
        NativeList<ArchetypeChunk> _queryChunks;

        protected override void OnCreate() {
            _query = SystemAPI.QueryBuilder().WithAll<CritterIndexInGroup, DrakeVisualEntitiesTransform, CritterMovementState>()
                .WithAll<CritterGroupSharedData>().WithNone<CulledEntityTag>().Build();

            movementStateHandle = SystemAPI.GetComponentTypeHandle<CritterMovementState>(true);
            groupSharedDataHandle = SystemAPI.GetSharedComponentTypeHandle<CritterGroupSharedData>();
            audioEmitterHandle = CheckedStateRef.EntityManager.GetComponentTypeHandle<StudioEventEmitter>(true);
            _queryChunks = new NativeList<ArchetypeChunk>(8, ARAlloc.Persistent);
        }

        protected override void OnDestroy() {
            _queryChunks.Dispose();
        }

        protected override void OnUpdate() {
            var deltaTime = SystemAPI.Time.DeltaTime;

            ref var state = ref CheckedStateRef;
            movementStateHandle.Update(ref state);
            groupSharedDataHandle.Update(ref state);
            audioEmitterHandle.Update(ref state);

            _query.ToArchetypeChunkList(_queryChunks);
            var queryChunksPtr = _queryChunks.GetUnsafeReadOnlyPtr();
            var entityManager = state.EntityManager;
            for (int chunkIndex = 0; chunkIndex < _queryChunks.Length; chunkIndex++) {
                ref var chunk = ref queryChunksPtr[chunkIndex];
                var movementStates = chunk.GetComponentDataPtrRO(ref movementStateHandle);
                var groupSharedData = chunk.GetSharedComponent(groupSharedDataHandle);
                var audioEmitters = chunk.GetManagedComponentAccessor(ref audioEmitterHandle, entityManager);
                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var movementState = movementStates[i];
                    var prevIsMoving = movementState.PrevIsMoving;
                    var isMoving = movementState.IsMoving;
                    if (isMoving & !prevIsMoving) {
                        var audioEmitter = audioEmitters[i];
                        //audioEmitter.PlayNewEventWithPauseTracking(new EventReference() { Guid = groupSharedData.sounds.movementSoundGuid });
                    } else if (!isMoving & prevIsMoving) {
                        var soundEmitter = audioEmitters[i];
                        //soundEmitter.PlayNewEventWithPauseTracking(new EventReference() { Guid = groupSharedData.sounds.idleSoundGuid });
                    }
                }
            }
        }
    }
}