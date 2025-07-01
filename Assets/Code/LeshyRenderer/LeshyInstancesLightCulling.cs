using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.TG.LeshyRenderer {
    [Il2CppEagerStaticClassConstruction]
    public static class LeshyInstancesLightCulling {
        static readonly UniversalProfilerMarker PerformLightCullingMarker = new UniversalProfilerMarker("Leshy.PerformLightCulling");

        public static JobHandle PerformLightCulling(in BatchCullingContext cullingContext, ref LeshyCells cells,
            UnsafeBitmask spawnedCells, UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms, JobHandle dependency) {
            PerformLightCullingMarker.Begin();

            spawnedCells.ToIndicesOfOneArray(ARAlloc.TempJob, out var upIndices);

            var clearInvisible = new LightClearInvisibleInstancesJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = upIndices,
                renderingLayers = cells.renderingLayers,
                hasShadows = cells.hasShadows,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(upIndices.LengthInt, dependency);

            var spawnedCount = spawnedCells.CountOnes();
            var lightInstances = new NativeList<int>((int)spawnedCount, ARAlloc.TempJob);

            var filterHandle = new LightInstancesFilterJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = upIndices,
                renderingLayers = cells.renderingLayers,
                hasShadows = cells.hasShadows,
            }.ScheduleAppend(lightInstances, upIndices.LengthInt, dependency);

            CullingUtils.LightCullingSetup(cullingContext, out var receiverSphereCuller, out var frustumPlanes,
                out var frustumSplits, out var receivers, out var lightFacingFrustumPlanes);

            // TODO: We should output in which splits each instance is visible and write it down to drawcall
            // Currently we render int to all splits, it's not a big deal but it's could be more optimal
            var cullingHandle = new LightCullInstancesJob {
                cullingPlanes = frustumPlanes, // Job will deallocate
                frustumSplits = frustumSplits, // Job will deallocate
                receiversPlanes = receivers, // Job will deallocate

                lightFacingFrustumPlanes = lightFacingFrustumPlanes, // Job will deallocate
                spheresSplitInfos = receiverSphereCuller.splitInfos, // Job will deallocate
                worldToLightSpaceRotation = receiverSphereCuller.worldToLightSpaceRotation,

                spawnedCells = upIndices,
                spawnedCellsIndices = lightInstances.AsDeferredJobArray(),

                aabbCenterXs = cells.aabbCenterXs,
                aabbCenterYs = cells.aabbCenterYs,
                aabbCenterZs = cells.aabbCenterZs,
                radii = cells.prefabsRadii,
                cellTransforms = cellTransforms,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(lightInstances, 1, filterHandle);
            cullingHandle = lightInstances.Dispose(cullingHandle);

            dependency = JobHandle.CombineDependencies(clearInvisible, cullingHandle);
            dependency = upIndices.Dispose(dependency);
            PerformLightCullingMarker.End();

            return dependency;
        }

        [BurstCompile]
        struct LightClearInvisibleInstancesJob : IJobFor {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;
            [ReadOnly] public UnsafeBitArray hasShadows;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                var isLayerCulled = (cullingLayerMask & renderingLayers[cellIndex]) == 0;
                var cellHasShadows = hasShadows.IsSet((int)cellIndex);
                if (isLayerCulled || !cellHasShadows) {
                    ref var visibility = ref frustumInstancesVisibility[cellIndex];
                    visibility.Clear();
                }
            }
        }

        [BurstCompile]
        struct LightInstancesFilterJob : IJobFilter {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;
            [ReadOnly] public UnsafeBitArray hasShadows;

            public bool Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                return (cullingLayerMask & renderingLayers[cellIndex]) != 0 && // Layer mask
                       hasShadows.IsSet((int)cellIndex); // Has shadows
            }
        }

        [BurstCompile]
        struct LightCullInstancesJob : IJobParallelForDefer {
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float4> cullingPlanes;
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<int> frustumSplits;
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float4> receiversPlanes;

            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<float4> lightFacingFrustumPlanes;
            [DeallocateOnJobCompletion, ReadOnly] public NativeArray<SphereSplitInfo> spheresSplitInfos;
            [ReadOnly] public float3x3 worldToLightSpaceRotation;

            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeArray<int> spawnedCellsIndices;

            [ReadOnly] public UnsafeArray<float> aabbCenterXs;
            [ReadOnly] public UnsafeArray<float> aabbCenterYs;
            [ReadOnly] public UnsafeArray<float> aabbCenterZs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var cellIndex = spawnedCells[(uint)spawnedCellsIndices[index]];

                var transforms = cellTransforms[cellIndex];
                var radius = radii[cellIndex];
                var aabbCenter = new float3(aabbCenterXs[cellIndex], aabbCenterYs[cellIndex], aabbCenterZs[cellIndex]);

                ref var visibility = ref frustumInstancesVisibility[cellIndex];

                for (uint i = 0; transforms.Length - i >= 4; i += 4) {
                    var t0 = transforms[i+0];
                    var t1 = transforms[i+1];
                    var t2 = transforms[i+2];
                    var t3 = transforms[i+3];

                    var scale0 = math.cmax(t0.scale);
                    var scale1 = math.cmax(t1.scale);
                    var scale2 = math.cmax(t2.scale);
                    var scale3 = math.cmax(t3.scale);

                    var rotation0 = t0.rotation;
                    var rotation1 = t1.rotation;
                    var rotation2 = t2.rotation;
                    var rotation3 = t3.rotation;

                    var aabbCenter0 = math.mul(rotation0, aabbCenter * scale0);
                    var aabbCenter1 = math.mul(rotation1, aabbCenter * scale1);
                    var aabbCenter2 = math.mul(rotation2, aabbCenter * scale2);
                    var aabbCenter3 = math.mul(rotation3, aabbCenter * scale3);

                    var subXs = new float4(
                        t0.position.x + aabbCenter0.x,
                        t1.position.x + aabbCenter1.x,
                        t2.position.x + aabbCenter2.x,
                        t3.position.x + aabbCenter3.x
                        );
                    var subYs = new float4(
                        t0.position.y + aabbCenter0.y,
                        t1.position.y + aabbCenter1.y,
                        t2.position.y + aabbCenter2.y,
                        t3.position.y + aabbCenter3.y
                        );
                    var subZs = new float4(
                        t0.position.z + aabbCenter0.z,
                        t1.position.z + aabbCenter1.z,
                        t2.position.z + aabbCenter2.z,
                        t3.position.z + aabbCenter3.z
                        );

                    var subRadii = new float4(scale0, scale1, scale2, scale3);
                    subRadii *= radius;

                    CullingUtils.LightSimdCulling(receiversPlanes, frustumSplits, cullingPlanes,
                        worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes,
                        subXs, subYs, subZs, subRadii,
                        out var mask);

                    visibility.SetBits((int)i, (ulong)math.bitmask(mask != 0), 4);
                }

                for (uint i = transforms.Length.SimdTrailing(); i < transforms.Length; ++i) {
                    var matrix = transforms[i];
                    var position = matrix.position;
                    var scale = math.cmax(matrix.scale);
                    var rotation = matrix.rotation;
                    position += math.mul(rotation, aabbCenter * scale);
                    var r = radius * scale;

                    CullingUtils.LightCulling(receiversPlanes, frustumSplits, cullingPlanes,
                        worldToLightSpaceRotation, spheresSplitInfos, lightFacingFrustumPlanes,
                        position, r, out var mask);

                    visibility.Set((int)i, mask != 0);
                }
            }
        }
    }
}
