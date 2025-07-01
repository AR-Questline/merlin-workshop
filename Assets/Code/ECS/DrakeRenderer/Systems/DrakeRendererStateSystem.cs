using Awaken.ECS.DrakeRenderer.Components;
using Awaken.Utility.Debugging;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Systems {
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class DrakeRendererStateSystem : SystemBase {
        EntityQuery _possibleToChangeQuery;
        EntityQuery _orthoToLoadQuery;
        EntityQuery _frozenLODQuery;
        EntityQuery _inLoadingQuery;
        BeginPresentationEntityCommandBufferSystem.Singleton _presentationEcbSingleton;
        
        ComponentTypeHandle<LODWorldReferencePoint> _lodReferencePointHandle;
        ComponentTypeHandle<DrakeRendererVisibleRangeComponent> _visibilityRangesHandle;
        EntityTypeHandle _entitiesHandles;
        DrakeStateJob _stateJob;
        
        public bool IsLoadingAny => _inLoadingQuery.IsEmpty == false;
        

        public static void PushSystemFreeze() {
            var ecsWorld = World.DefaultGameObjectInjectionWorld;
            var drakeState = ecsWorld.GetExistingSystemManaged<DrakeRendererStateSystem>();
            if (ecsWorld.EntityManager.HasComponent<FreezeDrakeStates>(drakeState.SystemHandle)) {
                ref var freeze = ref ecsWorld.EntityManager.GetComponentDataRW<FreezeDrakeStates>(drakeState.SystemHandle).ValueRW;
                ++freeze.counter;
            } else {
                var freezeDrakeStates = new FreezeDrakeStates { counter = 1 };
                ecsWorld.EntityManager.AddComponentData(drakeState.SystemHandle, freezeDrakeStates);
            }
        }

        public static void PopSystemFreeze() {
            var ecsWorld = World.DefaultGameObjectInjectionWorld;
            var drakeState = ecsWorld.GetExistingSystemManaged<DrakeRendererStateSystem>();
            if (ecsWorld.EntityManager.HasComponent<FreezeDrakeStates>(drakeState.SystemHandle)) {
                ref var freeze = ref ecsWorld.EntityManager.GetComponentDataRW<FreezeDrakeStates>(drakeState.SystemHandle).ValueRW;
                --freeze.counter;
                if (freeze.counter == 0) {
                    ecsWorld.EntityManager.RemoveComponent<FreezeDrakeStates>(drakeState.SystemHandle);
                }
            } else {
                Log.Critical?.Error("Trying to pop freeze system when it's not frozen");
            }
        }

        public void FreezeLoadedInstancesLOD() {
            EntityManager.AddComponent<DrakeFreezeLODTag>(_possibleToChangeQuery);
        }

        public void UnfreezeInstancesLOD() {
            EntityManager.RemoveComponent<DrakeFreezeLODTag>(_frozenLODQuery);
        }
        
        protected override void OnCreate() {
            _possibleToChangeQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<DrakeMeshMaterialComponent>(),
                    ComponentType.ReadOnly<LODWorldReferencePoint>(),
                    ComponentType.ReadOnly<DrakeRendererVisibleRangeComponent>(),
                },
                Absent = new[] {
                    ComponentType.ReadOnly<DrakeRendererUnloadRequestTag>(),
                    ComponentType.ReadOnly<DrakeRendererLoadRequestTag>(),
                    ComponentType.ReadOnly<DrakeRendererLoadingTag>(),
                    ComponentType.ReadOnly<DrakeRendererManualTag>(),
                    ComponentType.ReadOnly<DrakeFreezeLODTag>()
                },
            });
            _frozenLODQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<DrakeMeshMaterialComponent>(),
                    ComponentType.ReadOnly<LODWorldReferencePoint>(),
                    ComponentType.ReadOnly<DrakeRendererVisibleRangeComponent>(),
                    ComponentType.ReadOnly<DrakeFreezeLODTag>()
                }
            });
            

            _orthoToLoadQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<DrakeMeshMaterialComponent>(),
                    ComponentType.ReadOnly<LODWorldReferencePoint>(),
                    ComponentType.ReadOnly<DrakeRendererVisibleRangeComponent>(),
                },
                Absent = new[] {
                    ComponentType.ReadOnly<DrakeRendererUnloadRequestTag>(),
                    ComponentType.ReadOnly<DrakeRendererLoadRequestTag>(),
                    ComponentType.ReadOnly<DrakeRendererLoadingTag>(),
                    ComponentType.ReadOnly<DrakeRendererManualTag>(),
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                },
            });

            _inLoadingQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<DrakeMeshMaterialComponent>(),
                    ComponentType.ReadOnly<LODWorldReferencePoint>(),
                    ComponentType.ReadOnly<DrakeRendererVisibleRangeComponent>(),
                },
                Any = new [] {
                    ComponentType.ReadOnly<DrakeRendererLoadingTag>(),
                    ComponentType.ReadOnly<DrakeRendererLoadRequestTag>(),
                }
            });

            _presentationEcbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>(); 
            
            _lodReferencePointHandle = GetComponentTypeHandle<LODWorldReferencePoint>(true);
            _visibilityRangesHandle = GetComponentTypeHandle<DrakeRendererVisibleRangeComponent>(true);
            _entitiesHandles = GetEntityTypeHandle();
        }

        protected override void OnUpdate() {
            if (SystemAPI.HasComponent<FreezeDrakeStates>(SystemHandle)) {
                return;
            }

            var camera = Camera.main;
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                camera = sceneView ? sceneView.camera : null;
            }
#endif
            if (!camera) {
                return;
            }

            var lodParams = LODGroupExtensions.CalculateLODParams(camera);
            _entitiesHandles.Update(this);

            if (!lodParams.isOrtho) {
                var cameraPosition = lodParams.cameraPos;
                var distanceScaleSq = lodParams.distanceScale*lodParams.distanceScale;

                _lodReferencePointHandle.Update(this);
                _visibilityRangesHandle.Update(this);

                _stateJob = new DrakeStateJob {
                    cameraPosition = cameraPosition,
                    distanceScaleSq = distanceScaleSq,
                    commands = _presentationEcbSingleton.CreateCommandBuffer(World.Unmanaged).AsParallelWriter(),

                    lodReferencePointHandle = _lodReferencePointHandle,
                    visibleRangesHandle = _visibilityRangesHandle,
                    entitiesHandle = _entitiesHandles,
                };
                Dependency = _stateJob.ScheduleParallelByRef(_possibleToChangeQuery, Dependency);
            } else {
                Dependency = new DrakeOrthoStateJob {
                    commands = _presentationEcbSingleton.CreateCommandBuffer(World.Unmanaged).AsParallelWriter(),
                    entitiesHandle = _entitiesHandles,
                }.ScheduleParallel(_orthoToLoadQuery, Dependency);
            }
        }
    }

    [BurstCompile]
    struct DrakeStateJob : IJobChunk {
        public float3 cameraPosition;
        public float distanceScaleSq;
        public EntityCommandBuffer.ParallelWriter commands;

        [ReadOnly] public ComponentTypeHandle<LODWorldReferencePoint> lodReferencePointHandle;
        [ReadOnly] public ComponentTypeHandle<DrakeRendererVisibleRangeComponent> visibleRangesHandle;
        [ReadOnly] public EntityTypeHandle entitiesHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
            var lodReferencePoint = chunk.GetNativeArray(ref lodReferencePointHandle);
            var visibleRanges = chunk.GetNativeArray(ref visibleRangesHandle);
            var entities = chunk.GetNativeArray(entitiesHandle);

            if (chunk.Has<MaterialMeshInfo>()) {
                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var visibleRange = visibleRanges[i];

                    var instanceDistance = distanceScaleSq * math.lengthsq(cameraPosition - lodReferencePoint[i].Value);

                    if (visibleRange.value.x > instanceDistance || instanceDistance > visibleRange.value.y) {
                        commands.AddComponent<DrakeRendererUnloadRequestTag>(unfilteredChunkIndex, entities[i]);
                    }
                }
            } else {
                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var visibleRange = visibleRanges[i];

                    var instanceDistance = distanceScaleSq * math.lengthsq(cameraPosition - lodReferencePoint[i].Value);

                    if (visibleRange.value.x <= instanceDistance && instanceDistance <= visibleRange.value.y) {
                        commands.AddComponent<DrakeRendererLoadRequestTag>(unfilteredChunkIndex, entities[i]);
                    }
                }
            }
        }
    }

    [BurstCompile]
    struct DrakeOrthoStateJob : IJobChunk {
        public EntityCommandBuffer.ParallelWriter commands;

        [ReadOnly] public EntityTypeHandle entitiesHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
            var entities = chunk.GetNativeArray(entitiesHandle);

            for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                commands.AddComponent<DrakeRendererLoadRequestTag>(unfilteredChunkIndex, entities[i]);
            }
        }
    }
}
