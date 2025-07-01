using Awaken.ECS.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.Systems {
#if !UNITY_EDITOR
    [DisableAutoCreation]
#endif
    [UpdateInGroup(typeof(ARDebugSystemsGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial class CullingDistanceDebugStateSystem : SystemBase {
        EntityQuery _distancesQuery;

        ComponentTypeHandle<LODWorldReferencePoint> _lodReferencePointHandle;
        ComponentTypeHandle<CullingDistancePreviewComponent> _cullingDistancePreviewHandle;

        protected override void OnCreate() {
            _distancesQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[] {
                    ComponentType.ReadOnly<LODWorldReferencePoint>(),
                    ComponentType.ReadOnly<CullingDistancePreviewComponent>(),
                }
            });

            _lodReferencePointHandle = GetComponentTypeHandle<LODWorldReferencePoint>(true);
            _cullingDistancePreviewHandle = GetComponentTypeHandle<CullingDistancePreviewComponent>();
        }

        protected override void OnUpdate() {
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
            var cameraPosition = lodParams.cameraPos;
            var distanceScale = lodParams.distanceScale;

            _lodReferencePointHandle.Update(this);
            _cullingDistancePreviewHandle.Update(this);

            var job = new CullingDistanceJob {
                cameraPosition = cameraPosition,
                distanceScale = distanceScale,

                cullingDistancePreviewHandle = _cullingDistancePreviewHandle,
                lodReferencePointHandle = _lodReferencePointHandle,
            };
            Dependency = job.ScheduleParallel(_distancesQuery, Dependency);
        }

        [BurstCompile]
        struct CullingDistanceJob : IJobChunk {
            public float3 cameraPosition;
            public float distanceScale;

            [ReadOnly] public ComponentTypeHandle<LODWorldReferencePoint> lodReferencePointHandle;
            public ComponentTypeHandle<CullingDistancePreviewComponent> cullingDistancePreviewHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask) {
                var cullingDistancePreviews = chunk.GetNativeArray(ref cullingDistancePreviewHandle);
                var lodReferencePoints = chunk.GetNativeArray(ref lodReferencePointHandle);

                for (int i = 0, chunkEntityCount = chunk.Count; i < chunkEntityCount; i++) {
                    var cullingDistancePreview = cullingDistancePreviews[i];
                    var localDistance = distanceScale * math.length(cameraPosition - lodReferencePoints[i].Value);
                    cullingDistancePreview.localDistance = localDistance;
                    cullingDistancePreview.localDistanceSq = localDistance * localDistance;
                    cullingDistancePreviews[i] = cullingDistancePreview;
                }
            }
        }
    }
}
