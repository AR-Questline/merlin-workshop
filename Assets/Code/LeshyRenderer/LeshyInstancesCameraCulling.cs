using System.Runtime.CompilerServices;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.TG.LeshyRenderer {
    public static class LeshyInstancesCameraCulling {
        static readonly UniversalProfilerMarker PerformCameraCullingMarker = new UniversalProfilerMarker("Leshy.PerformCameraCulling");

        public static JobHandle PerformCameraCulling(in BatchCullingContext cullingContext, ref LeshyCells cells,
            UnsafeBitmask spawnedCells, UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms,
            bool forceGameCameraInSceneView, JobHandle dependency) {
            PerformCameraCullingMarker.Begin();

#if UNITY_EDITOR
            if (!forceGameCameraInSceneView)
#endif
            {
                for (int i = 0; i < 6; i++) {
                    cells.planes[(uint)i] = new float4(cullingContext.cullingPlanes[i].normal,
                        cullingContext.cullingPlanes[i].distance);
                }
                var lodParams = LODGroupExtensions.CalculateLODParams(cullingContext.lodParameters);
                cells.cameraPosition = lodParams.cameraPos;
            }

            var maxThreads = (float)JobsUtility.JobWorkerCount;
            var inputDependency = dependency;

            spawnedCells.ToIndicesOfOneArray(ARAlloc.TempJob, out var upIndices);

            // CalculatePossibleLodsJob
            dependency = new CalculatePossibleLodsJob {
                spawnedCells = upIndices,
                radii = cells.cellsRadii,
                cellsDistances = cells.cellsDistances,
                lodDistances = cells.lodDistances,
                possibleLods = cells.possibleLods,
            }.ScheduleParallel(upIndices.LengthInt, Mathf.CeilToInt(upIndices.Length/maxThreads), inputDependency);

            // Frustum and LOD
            var frustumAndLod = FrustumAndLod(cullingContext, cells, upIndices, cellTransforms, dependency);
            // Frustum
            var frustum = Frustum(cullingContext, cells, upIndices, cellTransforms, dependency);
            // LOD
            var lodsJob = Lod(cullingContext, cells, upIndices, cellTransforms, dependency);
            // Rewrite
            var rewriteJob = Rewrite(cullingContext, cells, upIndices, dependency);

            // Combine
            var partialDependencies = JobHandle.CombineDependencies(frustumAndLod, frustum);
            dependency = JobHandle.CombineDependencies(lodsJob, rewriteJob, partialDependencies);

            var clearInvisible = new ClearInvisibleInstancesJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = upIndices,
                renderingLayers = cells.renderingLayers,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(upIndices.LengthInt, inputDependency);

            dependency = JobHandle.CombineDependencies(dependency, clearInvisible);
            dependency = upIndices.Dispose(dependency);

            PerformCameraCullingMarker.End();
            return dependency;
        }

        static JobHandle FrustumAndLod(BatchCullingContext cullingContext, LeshyCells cells, UnsafeArray<uint> spawnedCells,
            UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms, JobHandle dependency) {
            var spawnedPartlyMixedLodVisible = new NativeList<int>(spawnedCells.LengthInt, ARAlloc.TempJob);

            var filterPartialVisibleMixedLod = new FilterPartialVisibleMixedLodCellsJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = spawnedCells,
                frustumFullCellVisibility = cells.frustumFullCellsVisibility,
                renderingLayers = cells.renderingLayers,
                possibleLods = cells.possibleLods,
            }.ScheduleAppend(spawnedPartlyMixedLodVisible, spawnedCells.LengthInt, dependency);

            var frustumAndLod = new InstancesFrustumAndLodJob {
                planes = cells.planes,
                cameraPosition = cells.cameraPosition,

                spawnedCells = spawnedCells,
                spawnedCellsIndices = spawnedPartlyMixedLodVisible.AsDeferredJobArray(),

                aabbCenterXs = cells.aabbCenterXs,
                aabbCenterYs = cells.aabbCenterYs,
                aabbCenterZs = cells.aabbCenterZs,
                radii = cells.prefabsRadii,
                lodDistancesSq = cells.lodDistancesSq,
                cellTransforms = cellTransforms,
                cellsInstances = cells.cellsInstances,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(spawnedPartlyMixedLodVisible, 1, filterPartialVisibleMixedLod);

            frustumAndLod = spawnedPartlyMixedLodVisible.Dispose(frustumAndLod);
            return frustumAndLod;
        }

        static JobHandle Frustum(BatchCullingContext cullingContext, LeshyCells cells, UnsafeArray<uint> spawnedCells,
            UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms, JobHandle dependency) {
            var spawnedPartlySingleLodVisible = new NativeList<int>(spawnedCells.LengthInt, ARAlloc.TempJob);

            var filterPartialVisibleSingleLod = new FilterPartialVisibleSingleLodCellsJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = spawnedCells,
                frustumFullCellVisibility = cells.frustumFullCellsVisibility,
                renderingLayers = cells.renderingLayers,
                possibleLods = cells.possibleLods,
            }.ScheduleAppend(spawnedPartlySingleLodVisible, spawnedCells.LengthInt, dependency);

            var frustum = new InstancesFrustumJob {
                planes = cells.planes,

                spawnedCells = spawnedCells,
                spawnedCellsIndices = spawnedPartlySingleLodVisible.AsDeferredJobArray(),

                aabbCenterXs = cells.aabbCenterXs,
                aabbCenterYs = cells.aabbCenterYs,
                aabbCenterZs = cells.aabbCenterZs,
                radii = cells.prefabsRadii,
                possibleLods = cells.possibleLods,
                cellTransforms = cellTransforms,
                cellsInstances = cells.cellsInstances,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(spawnedPartlySingleLodVisible, 1, filterPartialVisibleSingleLod);

            frustum = spawnedPartlySingleLodVisible.Dispose(frustum);
            return frustum;
        }

        static JobHandle Rewrite(BatchCullingContext cullingContext, LeshyCells cells, UnsafeArray<uint> spawnedCells,
            JobHandle dependency) {
            var spawnedFullySingleLodVisible = new NativeList<int>(spawnedCells.LengthInt, ARAlloc.TempJob);

            var filterFullVisibleCellsSingleLod = new FilterFullVisibleCellsSingleLodCellsJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = spawnedCells,
                frustumFullCellVisibility = cells.frustumFullCellsVisibility,
                renderingLayers = cells.renderingLayers,
                possibleLods = cells.possibleLods,
            }.ScheduleAppend(spawnedFullySingleLodVisible, spawnedCells.LengthInt, dependency);

            var rewriteJob = new InstancesRewriteJob {
                spawnedCells = spawnedCells,
                spawnedCellsIndices = spawnedFullySingleLodVisible.AsDeferredJobArray(),

                possibleLods = cells.possibleLods,
                cellsInstances = cells.cellsInstances,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(spawnedFullySingleLodVisible, 128, filterFullVisibleCellsSingleLod);

            rewriteJob = spawnedFullySingleLodVisible.Dispose(rewriteJob);
            return rewriteJob;
        }

        static JobHandle Lod(BatchCullingContext cullingContext, LeshyCells cells, UnsafeArray<uint> spawnedCells,
            UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms, JobHandle dependency) {
            var spawnedFullyMixedLodVisible = new NativeList<int>(spawnedCells.LengthInt, ARAlloc.TempJob);

            var filterFullVisibleMixedLodCellsJob = new FilterFullVisibleMixedLodCellsJob {
                cullingLayerMask = cullingContext.cullingLayerMask,

                spawnedCells = spawnedCells,
                frustumFullCellVisibility = cells.frustumFullCellsVisibility,
                renderingLayers = cells.renderingLayers,
                possibleLods = cells.possibleLods,
            }.ScheduleAppend(spawnedFullyMixedLodVisible, spawnedCells.LengthInt, dependency);

            var lodsJob = new InstancesLodJob {
                cameraPosition = cells.cameraPosition,

                spawnedCells = spawnedCells,
                spawnedCellsIndices = spawnedFullyMixedLodVisible.AsDeferredJobArray(),

                lodDistancesSq = cells.lodDistancesSq,
                cellTransforms = cellTransforms,
                cellsInstances = cells.cellsInstances,

                frustumInstancesVisibility = cells.perInstanceVisibility,
            }.Schedule(spawnedFullyMixedLodVisible, 2, filterFullVisibleMixedLodCellsJob);

            lodsJob = spawnedFullyMixedLodVisible.Dispose(lodsJob);
            return lodsJob;
        }

        [BurstCompile]
        struct ClearInvisibleInstancesJob : IJobFor {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                var isLayerCulled = (cullingLayerMask & renderingLayers[cellIndex]) == 0;
                if (isLayerCulled) {
                    frustumInstancesVisibility[cellIndex].Clear();
                }
            }
        }

        [BurstCompile]
        struct CalculatePossibleLodsJob : IJobFor {
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<float> cellsDistances;
            [ReadOnly] public UnsafeArray<float4x2> lodDistances;

            [WriteOnly] public UnsafeArray<byte> possibleLods;

            public void Execute(int index) {
                uint i = spawnedCells[(uint)index];

                var radius = radii[i];
                var distance = cellsDistances[i];

                var distanceRange = new float2(distance-radius, distance+radius);

                byte possible = 0;

                var lodDist = lodDistances[i];
                var lodStart = 0f;
                for (int j = 0; j < 8; j++) {
                    var value = lodDist.Get(j);
                    var lodRange = new float2(lodStart, value);
                    lodStart = value;
                    if (RangesOverlaps(distanceRange, lodRange)) {
                        possible |= (byte)(1 << j);
                    }
                }
                possibleLods[i] = possible;
            }

            static bool RangesOverlaps(float2 a, float2 b) {
                return a.x <= b.y && a.y >= b.x;
            }
        }

        [BurstCompile]
        struct FilterPartialVisibleSingleLodCellsJob : IJobFilter {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeBitArray frustumFullCellVisibility;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;
            [ReadOnly] public UnsafeArray<byte> possibleLods;

            public bool Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                return (cullingLayerMask & renderingLayers[cellIndex]) != 0 && // Layer mask
                       !frustumFullCellVisibility.IsSet((int)cellIndex) && // Not fully visible
                       math.countbits((int)possibleLods[cellIndex]) == 1; // Single LOD possible
            }
        }

        [BurstCompile]
        struct FilterPartialVisibleMixedLodCellsJob : IJobFilter {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeBitArray frustumFullCellVisibility;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;
            [ReadOnly] public UnsafeArray<byte> possibleLods;

            public bool Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                return (cullingLayerMask & renderingLayers[cellIndex]) != 0 && // Layer mask
                       !frustumFullCellVisibility.IsSet((int)cellIndex) && // Not fully visible
                       math.countbits((int)possibleLods[cellIndex]) > 1; // Multiple LOD possible
            }
        }

        [BurstCompile]
        struct FilterFullVisibleCellsSingleLodCellsJob : IJobFilter {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeBitArray frustumFullCellVisibility;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;
            [ReadOnly] public UnsafeArray<byte> possibleLods;

            public bool Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                return (cullingLayerMask & renderingLayers[cellIndex]) != 0 && // Layer mask
                       frustumFullCellVisibility.IsSet((int)cellIndex) && // Fully visible
                       math.countbits((int)possibleLods[cellIndex]) == 1;
            }
        }

        [BurstCompile]
        struct FilterFullVisibleMixedLodCellsJob : IJobFilter {
            public uint cullingLayerMask;
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeBitArray frustumFullCellVisibility;
            [ReadOnly] public UnsafeArray<uint> renderingLayers;
            [ReadOnly] public UnsafeArray<byte> possibleLods;

            public bool Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                return (cullingLayerMask & renderingLayers[cellIndex]) != 0 && // Layer mask
                       frustumFullCellVisibility.IsSet((int)cellIndex) &&
                       math.countbits((int)possibleLods[cellIndex]) > 1; // Multiple LOD possible
            }
        }

        [BurstCompile]
        struct InstancesFrustumAndLodJob : IJobParallelForDefer {
            [ReadOnly] public UnsafeArray<float4> planes;
            [ReadOnly] public float3 cameraPosition;

            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeArray<int> spawnedCellsIndices;

            [ReadOnly] public UnsafeArray<float> aabbCenterXs;
            [ReadOnly] public UnsafeArray<float> aabbCenterYs;
            [ReadOnly] public UnsafeArray<float> aabbCenterZs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<float4x2> lodDistancesSq;
            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms;
            [ReadOnly] public UnsafeArray<RenderingInstancesHandle> cellsInstances;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var p0 = planes[0];
                var p1 = planes[1];
                var p2 = planes[2];
                var p3 = planes[3];
                var p4 = planes[4];
                var p5 = planes[5];

                var cellIndex = spawnedCells[(uint)spawnedCellsIndices[index]];
                var transforms = cellTransforms[cellIndex];
                var radius = radii[cellIndex];
                var lodDistances = lodDistancesSq[cellIndex];
                var cellInstances = cellsInstances[cellIndex];
                var aabbCenter = new float3(aabbCenterXs[cellIndex], aabbCenterYs[cellIndex], aabbCenterZs[cellIndex]);

                ref var visibility = ref frustumInstancesVisibility[cellIndex];

                for (uint i = 0; transforms.Length - i >= 4; i += 4) {
                    var t0 = transforms[i+0];
                    var t1 = transforms[i+1];
                    var t2 = transforms[i+2];
                    var t3 = transforms[i+3];

                    // Frustum
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

                    bool4 frustumMask =
                        p0.x * subXs + p0.y * subYs + p0.z * subZs + p0.w + subRadii > 0.0f &
                        p1.x * subXs + p1.y * subYs + p1.z * subZs + p1.w + subRadii > 0.0f &
                        p2.x * subXs + p2.y * subYs + p2.z * subZs + p2.w + subRadii > 0.0f &
                        p3.x * subXs + p3.y * subYs + p3.z * subZs + p3.w + subRadii > 0.0f &
                        p4.x * subXs + p4.y * subYs + p4.z * subZs + p4.w + subRadii > 0.0f &
                        p5.x * subXs + p5.y * subYs + p5.z * subZs + p5.w + subRadii > 0.0f;

                    visibility.SetBits((int)i, (ulong)math.bitmask(frustumMask), 4);

                    // LOD
                    CalculateSimdLods(i, cameraPosition, subXs, subYs, subZs, lodDistances, cellInstances);
                }

                for (uint i = transforms.Length.SimdTrailing(); i < transforms.Length; ++i) {
                    var matrix = transforms[i];
                    var position = matrix.position;
                    var scale = math.cmax(matrix.scale);
                    var rotation = matrix.rotation;
                    position += math.mul(rotation, aabbCenter * scale);
                    var r = radius * scale;

                    bool frustumVisible =
                        math.dot(p0.xyz, position) + p0.w + r > 0.0f &&
                        math.dot(p1.xyz, position) + p1.w + r > 0.0f &&
                        math.dot(p2.xyz, position) + p2.w + r > 0.0f &&
                        math.dot(p3.xyz, position) + p3.w + r > 0.0f &&
                        math.dot(p4.xyz, position) + p4.w + r > 0.0f &&
                        math.dot(p5.xyz, position) + p5.w + r > 0.0f;

                    visibility.Set((int)i, frustumVisible);

                    CalculateSingleLod(i, position, cameraPosition, lodDistances, cellInstances);
                }
            }
        }

        [BurstCompile]
        unsafe struct InstancesFrustumJob : IJobParallelForDefer {
            [ReadOnly] public UnsafeArray<float4> planes;

            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeArray<int> spawnedCellsIndices;

            [ReadOnly] public UnsafeArray<float> aabbCenterXs;
            [ReadOnly] public UnsafeArray<float> aabbCenterYs;
            [ReadOnly] public UnsafeArray<float> aabbCenterZs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<byte> possibleLods;
            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms;
            [ReadOnly] public UnsafeArray<RenderingInstancesHandle> cellsInstances;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var p0 = planes[0];
                var p1 = planes[1];
                var p2 = planes[2];
                var p3 = planes[3];
                var p4 = planes[4];
                var p5 = planes[5];

                var cellIndex = spawnedCells[(uint)spawnedCellsIndices[index]];
                var cellInstances = cellsInstances[cellIndex];

                // Lods
                var lod = possibleLods[cellIndex];
                UnsafeUtility.MemSet(cellInstances.instancesSelectedLod.Ptr, lod, cellInstances.instancesSelectedLod.Length);

                // Frustum
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

                    bool4 frustumMask =
                        p0.x * subXs + p0.y * subYs + p0.z * subZs + p0.w + subRadii > 0.0f &
                        p1.x * subXs + p1.y * subYs + p1.z * subZs + p1.w + subRadii > 0.0f &
                        p2.x * subXs + p2.y * subYs + p2.z * subZs + p2.w + subRadii > 0.0f &
                        p3.x * subXs + p3.y * subYs + p3.z * subZs + p3.w + subRadii > 0.0f &
                        p4.x * subXs + p4.y * subYs + p4.z * subZs + p4.w + subRadii > 0.0f &
                        p5.x * subXs + p5.y * subYs + p5.z * subZs + p5.w + subRadii > 0.0f;

                    visibility.SetBits((int)i, (ulong)math.bitmask(frustumMask), 4);
                }

                for (uint i = transforms.Length.SimdTrailing(); i < transforms.Length; ++i) {
                    var matrix = transforms[i];
                    var position = matrix.position;
                    var rotation = matrix.rotation;
                    var scale = math.cmax(matrix.scale);
                    position += math.mul(rotation, aabbCenter * scale);
                    var r = radius * scale;

                    bool frustumVisible =
                        math.dot(p0.xyz, position) + p0.w + r > 0.0f &&
                        math.dot(p1.xyz, position) + p1.w + r > 0.0f &&
                        math.dot(p2.xyz, position) + p2.w + r > 0.0f &&
                        math.dot(p3.xyz, position) + p3.w + r > 0.0f &&
                        math.dot(p4.xyz, position) + p4.w + r > 0.0f &&
                        math.dot(p5.xyz, position) + p5.w + r > 0.0f;

                    visibility.Set((int)i, frustumVisible);
                }
            }
        }

        [BurstCompile]
        struct InstancesLodJob : IJobParallelForDefer {
            [ReadOnly] public float3 cameraPosition;

            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeArray<int> spawnedCellsIndices;

            [ReadOnly] public UnsafeArray<float4x2> lodDistancesSq;
            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> cellTransforms;
            [ReadOnly] public UnsafeArray<RenderingInstancesHandle> cellsInstances;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var cellIndex = spawnedCells[(uint)spawnedCellsIndices[index]];

                var transforms = cellTransforms[cellIndex];
                var lodDistances = lodDistancesSq[cellIndex];
                var cellInstances = cellsInstances[cellIndex];

                // Cell fully visible
                ref var frustum = ref frustumInstancesVisibility[cellIndex];
                frustum.SetBits(0, true, frustum.Length);

                for (uint i = 0; transforms.Length - i >= 4; i += 4) {
                    var t0 = transforms[i+0];
                    var t1 = transforms[i+1];
                    var t2 = transforms[i+2];
                    var t3 = transforms[i+3];

                    var subXs = new float4(t0.position.x, t1.position.x, t2.position.x, t3.position.x);
                    var subYs = new float4(t0.position.y, t1.position.y, t2.position.y, t3.position.y);
                    var subZs = new float4(t0.position.z, t1.position.z, t2.position.z, t3.position.z);

                    CalculateSimdLods(i, cameraPosition, subXs, subYs, subZs, lodDistances, cellInstances);
                }

                for (uint i = transforms.Length.SimdTrailing(); i < transforms.Length; ++i) {
                    var matrix = transforms[i];
                    var pos = matrix.position;
                    CalculateSingleLod(i, pos, cameraPosition, lodDistances, cellInstances);
                }
            }
        }

        [BurstCompile]
        unsafe struct InstancesRewriteJob : IJobParallelForDefer {
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public NativeArray<int> spawnedCellsIndices;

            [ReadOnly] public UnsafeArray<byte> possibleLods;
            [ReadOnly] public UnsafeArray<RenderingInstancesHandle> cellsInstances;

            [WriteOnly] public UnsafeArray<UnsafeBitArray> frustumInstancesVisibility;

            public void Execute(int index) {
                var cellIndex = spawnedCells[(uint)spawnedCellsIndices[index]];

                var cellInstances = cellsInstances[cellIndex];

                // Cell fully visible
                ref var frustum = ref frustumInstancesVisibility[cellIndex];
                frustum.SetBits(0, true, frustum.Length);

                var lod = possibleLods[cellIndex];
                UnsafeUtility.MemSet(cellInstances.instancesSelectedLod.Ptr, lod, cellInstances.instancesSelectedLod.Length);
            }
        }

        // === Helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateSimdLods(uint i, float3 cameraPosition,
            in float4 subXs, in float4 subYs, in float4 subZs, in float4x2 lodDistances,
            in RenderingInstancesHandle cellInstances) {
            var xDiffs = subXs - cameraPosition.x;
            var yDiffs = subYs - cameraPosition.y;
            var zDiffs = subZs - cameraPosition.z;

            xDiffs = xDiffs * xDiffs;
            yDiffs = yDiffs * yDiffs;
            zDiffs = zDiffs * zDiffs;

            var distancesSq = xDiffs + yDiffs + zDiffs;

            for (int j = 0; j < 4; j++) {
                byte lod = CreateLodMask(lodDistances, distancesSq[j]);
                cellInstances.instancesSelectedLod[i + (uint)j] = lod;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CalculateSingleLod(uint i, float3 pos, float3 cameraPosition, in float4x2 lodDistances, in RenderingInstancesHandle cellInstances) {
            var distanceSq = math.distancesq(pos, cameraPosition);
            byte lod = CreateLodMask(lodDistances, distanceSq);
            cellInstances.instancesSelectedLod[i] = lod;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte CreateLodMask(in float4x2 lodDistances, float distanceSq) {
            byte lod = 0;
            var lodStart = 0f;
            for (int k = 0; k < 8; k++) {
                var value = lodDistances.Get(k);
                var lodRange = new float2(lodStart, value);
                lodStart = value;
                if (InRange(lodRange, distanceSq)) {
                    lod |= (byte)(1 << k);
                }
            }
            return lod;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool InRange(float2 range, float value) {
            return range.x <= value && range.y > value;
        }
    }
}
