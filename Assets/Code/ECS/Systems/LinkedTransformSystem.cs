using Awaken.ECS.Components;
using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace Awaken.ECS.Systems {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class LinkedTransformSystem : SystemBase {
        const int TransformsPreAllocationSize = 16384;
        const int FreeIdsCapacity = 1024;

        // Query to see more info in Systems window
        // ReSharper disable once NotAccessedField.Local
        EntityQuery _subjectsQuery;
        EntityQuery _friedQuery;
        EntityQuery _newQuery;

        BeginInitializationEntityCommandBufferSystem.Singleton _singleton;

        TransformAccessArray _transformsArray;
        NativeList<Entity> _linkedTransformEntities;
        NativeList<int> _freeIds;

        public int Register(Entity entity, Transform transform) {
            if (_freeIds.Length > 0) {
                var lastIndex = _freeIds.Length - 1;
                var index = _freeIds[lastIndex];
                _freeIds.RemoveAtSwapBack(lastIndex);
                _transformsArray[index] = transform;
                _linkedTransformEntities[index] = entity;
                return index;
            }
            var newId = _transformsArray.length;
            _transformsArray.Add(transform);
            _linkedTransformEntities.Add(entity);

            return newId;
        }

        protected override void OnCreate() {
            _transformsArray = new TransformAccessArray(TransformsPreAllocationSize);
            _linkedTransformEntities = new NativeList<Entity>(TransformsPreAllocationSize, ARAlloc.Persistent);
            _freeIds = new NativeList<int>(FreeIdsCapacity, ARAlloc.Persistent);

            _singleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            _subjectsQuery = GetEntityQuery(typeof(LocalToWorld), typeof(LinkedTransformIndexComponent));
            var queryBuilder = new EntityQueryBuilder(ARAlloc.Temp);

            _friedQuery = queryBuilder
                .WithAll<LinkedTransformIndexComponent>()
                .WithAbsent<LocalToWorld, LinkedTransformComponent>()
                .Build(this);
            queryBuilder.Reset();

            _newQuery = queryBuilder
                .WithAll<LinkedTransformComponent>()
                .WithAbsent<LinkedTransformIndexComponent>()
                .Build(this);
            queryBuilder.Dispose();
        }

        protected override void OnDestroy() {
            _transformsArray.Dispose();
            _linkedTransformEntities.Dispose();
            _freeIds.Dispose();
        }

        protected override void OnUpdate() {
            if (_newQuery.CalculateEntityCount() > 0) {
                var registerEcb = new EntityCommandBuffer(ARAlloc.Temp);

                var newEntities = _newQuery.ToEntityArray(ARAlloc.Temp);
                var newTransforms = _newQuery.ToComponentDataArray<LinkedTransformComponent>(ARAlloc.Temp);

                for (var i = 0; i < newEntities.Length; i++) {
                    var e = newEntities[i];
                    var newTransform = newTransforms[i];
                    if (newTransform.transform.IsValid()) {
                        var index = Register(e, newTransform.transform.Value);
                        registerEcb.AddComponent(e, new LinkedTransformIndexComponent(index));
                    } else {
                        registerEcb.RemoveComponent<LinkedTransformComponent>(e);
                    }
                }

                newEntities.Dispose();
                newTransforms.Dispose();

                registerEcb.Playback(EntityManager);
                registerEcb.Dispose();
            }

            var ecb = _singleton.CreateCommandBuffer(World.Unmanaged);
            var jobHandle = new ReleaseTransformsJob {
                ecb = ecb,
                freeIndices = _freeIds,
                linkedTransformEntities = _linkedTransformEntities.AsArray()
            }.Schedule(_friedQuery, Dependency);

            Dependency = new SyncPositionToEntityJob {
                entities = _linkedTransformEntities.AsArray(),
                localToWorldLookup = GetComponentLookup<LocalToWorld>(),
                offsetLookup = GetComponentLookup<LinkedTransformLocalToWorldOffsetComponent>(true)
            }.ScheduleReadOnly(_transformsArray, 128, jobHandle);
        }

        [BurstCompile]
        struct SyncPositionToEntityJob : IJobParallelForTransform {
            [ReadOnly] public NativeArray<Entity> entities;
            [ReadOnly, NativeDisableParallelForRestriction]
            public ComponentLookup<LinkedTransformLocalToWorldOffsetComponent> offsetLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalToWorld> localToWorldLookup;

            public void Execute(int index, [ReadOnly] TransformAccess transform) {
                Entity entity = entities[index];
                if (entity == Entity.Null) {
                    return;
                }
                if (!transform.isValid) {
                    return;
                }

                var localToWorld = transform.localToWorldMatrix;

                var entityLocalToWorld = localToWorldLookup.GetRefRW(entity);
                if (offsetLookup.HasComponent(entity)) {
                    entityLocalToWorld.ValueRW.Value = math.mul(localToWorld, offsetLookup[entity].offsetMatrix);
                } else {
                    entityLocalToWorld.ValueRW.Value = localToWorld;
                }
            }
        }

        [BurstCompile]
        partial struct ReleaseTransformsJob : IJobEntity {
            public EntityCommandBuffer ecb;
            public NativeList<int> freeIndices;
            public NativeArray<Entity> linkedTransformEntities;

            public void Execute(in LinkedTransformIndexComponent indexComponent) {
                freeIndices.Add(indexComponent.index);
                ecb.RemoveComponent<LinkedTransformIndexComponent>(linkedTransformEntities[indexComponent.index]);
                linkedTransformEntities[indexComponent.index] = Entity.Null;
            }
        }
    }
}