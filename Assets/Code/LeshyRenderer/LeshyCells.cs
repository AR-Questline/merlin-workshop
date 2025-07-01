using System;
using System.Diagnostics.CodeAnalysis;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Files;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.TG.LeshyRenderer {
    public struct LeshyCells : IMemorySnapshotProvider {
        static readonly Plane[] PlanesOop = new Plane[6];
        static readonly UniversalProfilerMarker CalculateCellsVisibilityMarker = new UniversalProfilerMarker("Leshy.CalculateCellsVisibility");

        CellVisibilityJob _cellVisibilityJob;
        public float3 cameraPosition;

        // === Allocations - have all allocated data next to each other for easier memory management
        public UnsafeArray<float4> planes;
        public UnsafeArray<CatalogCellData> cellsCatalog;
        // -- SoA
        // Persistent cell data
        public UnsafeArray<float> xs;
        public UnsafeArray<float> ys;
        public UnsafeArray<float> zs;
        public UnsafeArray<float> cellsRadii;
        public UnsafeArray<float> prefabsRadii;
        public UnsafeArray<float> spawnDistances;
        public UnsafeArray<float4x2> lodDistances;
        public UnsafeArray<float4x2> lodDistancesSq;
        public UnsafeArray<uint> renderingLayers;
        public UnsafeBitArray hasShadows;
        public UnsafeArray<float> aabbCenterXs;
        public UnsafeArray<float> aabbCenterYs;
        public UnsafeArray<float> aabbCenterZs;
        // Calculation outputs
        public UnsafeArray<float> cellsDistances;
        public NativeBitArray frustumPartialCellsVisibility;
        public NativeBitArray frustumFullCellsVisibility;
        public NativeBitArray distanceCellsVisibility;
        public NativeBitArray finalCellsVisibility;
        public UnsafeArray<byte> possibleLods;
        // Rendering ranges data
        public UnsafeArray<RenderingInstancesHandle> cellsInstances;
        public UnsafeArray<UnsafeBitArray> perInstanceVisibility;
        public UnsafeArray<UnsafeArray<float>.Span> perInstanceMipmapsFactor;

        public bool Enabled { get; set; }
        public int CellsCount => (int)cellsCatalog.Length;
        public bool Created => finalCellsVisibility is { IsCreated: true, Length: > 0 };

        // === Lifetime
        public void Init(string cellsCatalogPath, LeshyPrefabs.PrefabRuntime[] runtimePrefabs) {
            Enabled = true;
            LoadCellsCatalog(cellsCatalogPath);
            AllocateCollections();
            FillSavedCellsData(runtimePrefabs);
            FillPersistentJobsData();
        }

        public void Dispose() {
            DisposeAllocatedMemory();
        }

        void LoadCellsCatalog(string cellsCatalogPath) {
            cellsCatalog = GetCellCatalog(cellsCatalogPath, Allocator.Persistent);
        }

        void AllocateCollections() {
            planes = new UnsafeArray<float4>(6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var cellsCount = cellsCatalog.Length;

            xs = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ys = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            zs = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            cellsRadii = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            prefabsRadii = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            spawnDistances = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            lodDistances = new UnsafeArray<float4x2>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            lodDistancesSq = new UnsafeArray<float4x2>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            renderingLayers = new UnsafeArray<uint>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            hasShadows = new UnsafeBitArray((int)cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            aabbCenterXs = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            aabbCenterYs = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            aabbCenterZs = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            cellsDistances = new UnsafeArray<float>(cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            frustumPartialCellsVisibility = new NativeBitArray((int)cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            frustumFullCellsVisibility = new NativeBitArray((int)cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            distanceCellsVisibility = new NativeBitArray((int)cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            finalCellsVisibility = new NativeBitArray((int)cellsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            possibleLods = new UnsafeArray<byte>(cellsCount, Allocator.Persistent);

            cellsInstances = new UnsafeArray<RenderingInstancesHandle>(cellsCount, Allocator.Persistent);
            perInstanceVisibility = new UnsafeArray<UnsafeBitArray>(cellsCount, Allocator.Persistent);
            perInstanceMipmapsFactor = new UnsafeArray<UnsafeArray<float>.Span>(cellsCount, Allocator.Persistent);
        }

        void FillSavedCellsData(LeshyPrefabs.PrefabRuntime[] runtimePrefabs) {
            for (uint i = 0; i < this.cellsCatalog.Length; i++) {
                var cellData = cellsCatalog[i];
                var cellCenter = cellData.bounds.Center;
                var cellRadius = math.length(cellData.bounds.Extents);

                xs[i] = cellCenter.x;
                ys[i] = cellCenter.y;
                zs[i] = cellCenter.z;
                cellsRadii[i] = cellRadius;

                var prefab = runtimePrefabs[cellData.prefabId];

                lodDistances[i] = prefab.lodDistances;
                lodDistancesSq[i] = lodDistances[i] * lodDistances[i];
                renderingLayers[i] = unchecked((uint)(1 << prefab.renderers[0].filterSettings.layer));
                hasShadows.Set((int)i, prefab.renderers[0].filterSettings.shadowCastingMode != ShadowCastingMode.Off);

                aabbCenterXs[i] = prefab.localBounds.Center.x;
                aabbCenterYs[i] = prefab.localBounds.Center.y;
                aabbCenterZs[i] = prefab.localBounds.Center.z;

                var spawnDistance = 0f;
                for (var j = 0; j < 8; j++) {
                    var value = lodDistances[i].Get(j);
                    if (float.IsFinite(value)) {
                        spawnDistance = value;
                    }
                }

                spawnDistances[i] = spawnDistance + cellRadius * 1.05f; // We are lagging behind the camera a bit

                prefabsRadii[i] = math.length(prefab.localBounds.Extents);
            }
        }

        void FillPersistentJobsData() {
            _cellVisibilityJob.planes = planes;

            _cellVisibilityJob.xs = xs;
            _cellVisibilityJob.ys = ys;
            _cellVisibilityJob.zs = zs;
            _cellVisibilityJob.radii = cellsRadii;
            _cellVisibilityJob.spawnDistances = spawnDistances;

            _cellVisibilityJob.cellsDistances = cellsDistances;
            _cellVisibilityJob.frustumPartialCellsVisibility = frustumPartialCellsVisibility;
            _cellVisibilityJob.frustumFullCellsVisibility = frustumFullCellsVisibility;
            _cellVisibilityJob.distanceCellsVisibility = distanceCellsVisibility;
            _cellVisibilityJob.outputCellsVisibility = finalCellsVisibility;
        }

        void DisposeAllocatedMemory() {
            planes.Dispose();

            cellsCatalog.Dispose();

            xs.Dispose();
            ys.Dispose();
            zs.Dispose();
            cellsRadii.Dispose();
            prefabsRadii.Dispose();
            spawnDistances.Dispose();
            lodDistances.Dispose();
            lodDistancesSq.Dispose();
            renderingLayers.Dispose();
            hasShadows.Dispose();
            aabbCenterXs.Dispose();
            aabbCenterYs.Dispose();
            aabbCenterZs.Dispose();

            cellsDistances.Dispose();
            frustumPartialCellsVisibility.Dispose();
            frustumFullCellsVisibility.Dispose();
            distanceCellsVisibility.Dispose();
            finalCellsVisibility.Dispose();
            possibleLods.Dispose();

            for (uint i = 0; i < cellsInstances.Length; i++) {
                cellsInstances[i].Dispose();
            }
            cellsInstances.Dispose();
            perInstanceVisibility.Dispose();
            perInstanceMipmapsFactor.Dispose();
        }

        // === Cells
        public void CalculateCellsVisibility(Camera camera) {
            CalculateCellsVisibilityMarker.Begin();

            var cameraTransform = camera.transform;
            cameraPosition = cameraTransform.position;
            GeometryUtility.CalculateFrustumPlanes(camera, PlanesOop);

            for (uint i = 0; i < 6; ++i) {
                planes[i] = new float4(PlanesOop[i].normal, PlanesOop[i].distance);
            }

            _cellVisibilityJob.cameraPosition = cameraPosition;
            if (Enabled) {
                _cellVisibilityJob.RunByRef();
            }

            CalculateCellsVisibilityMarker.End();
        }

        public void AddAllocatedCell(uint cellIndex, in RenderingInstancesHandle handle) {
            cellsInstances[cellIndex] = handle;
            perInstanceVisibility[cellIndex] = handle.instanceVisibilities;
            perInstanceMipmapsFactor[cellIndex] = handle.mipmapsFactors;
        }

        public void DespawnCell(uint cellIndex) {
            cellsInstances[cellIndex].Dispose();
            cellsInstances[cellIndex] = default;
        }

        public static UnsafeArray<CatalogCellData> GetCellCatalog(string cellsCatalogPath, Allocator allocator) {
            return FileRead.ToNewBuffer<CatalogCellData>(cellsCatalogPath, allocator);
        }
        
        [BurstCompile]
        struct CellVisibilityJob : IJob {
            public float3 cameraPosition;
            [ReadOnly] public UnsafeArray<float4> planes;

            [ReadOnly] public UnsafeArray<float> xs;
            [ReadOnly] public UnsafeArray<float> ys;
            [ReadOnly] public UnsafeArray<float> zs;
            [ReadOnly] public UnsafeArray<float> radii;
            [ReadOnly] public UnsafeArray<float> spawnDistances;

            [WriteOnly] public UnsafeArray<float> cellsDistances;
            [WriteOnly] public NativeBitArray frustumPartialCellsVisibility;
            [WriteOnly] public NativeBitArray frustumFullCellsVisibility;
            [WriteOnly] public NativeBitArray distanceCellsVisibility;
            [WriteOnly] public NativeBitArray outputCellsVisibility;

            public void Execute() {
                var p0 = planes[0];
                var p1 = planes[1];
                var p2 = planes[2];
                var p3 = planes[3];
                var p4 = planes[4];
                var p5 = planes[5];

                for (uint i = 0; radii.Length - i >= 4; i += 4) {
#if UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS
                    Unity.Burst.CompilerServices.Loop.ExpectVectorized();
#endif
                    // === Frustum
                    var subXs = xs.ReinterpretLoad<float4>(i);
                    var subYs = ys.ReinterpretLoad<float4>(i);
                    var subZs = zs.ReinterpretLoad<float4>(i);

                    var subRadii = radii.ReinterpretLoad<float4>(i);

                    // Test each of the 6 planes against the 4 shuffled spheres
                    var dotPlane0 = p0.x * subXs + p0.y * subYs + p0.z * subZs + p0.w;
                    var dotPlane1 = p1.x * subXs + p1.y * subYs + p1.z * subZs + p1.w;
                    var dotPlane2 = p2.x * subXs + p2.y * subYs + p2.z * subZs + p2.w;
                    var dotPlane3 = p3.x * subXs + p3.y * subYs + p3.z * subZs + p3.w;
                    var dotPlane4 = p4.x * subXs + p4.y * subYs + p4.z * subZs + p4.w;
                    var dotPlane5 = p5.x * subXs + p5.y * subYs + p5.z * subZs + p5.w;

                    bool4 fullVisibleMask = dotPlane0 - subRadii > 0.0f &
                                            dotPlane1 - subRadii > 0.0f &
                                            dotPlane2 - subRadii > 0.0f &
                                            dotPlane3 - subRadii > 0.0f &
                                            dotPlane4 - subRadii > 0.0f &
                                            dotPlane5 - subRadii > 0.0f;

                    bool4 partialVisibleMask = dotPlane0 + subRadii > 0.0f &
                                               dotPlane1 + subRadii > 0.0f &
                                               dotPlane2 + subRadii > 0.0f &
                                               dotPlane3 + subRadii > 0.0f &
                                               dotPlane4 + subRadii > 0.0f &
                                               dotPlane5 + subRadii > 0.0f;

                    frustumPartialCellsVisibility.SetBits((int)i, (ulong)math.bitmask(partialVisibleMask), 4);
                    frustumFullCellsVisibility.SetBits((int)i, (ulong)math.bitmask(fullVisibleMask), 4);

                    // === Distance
                    var subSpawnDistances = spawnDistances.ReinterpretLoad<float4>(i);
                    var xDiffs = subXs - cameraPosition.x;
                    var yDiffs = subYs - cameraPosition.y;
                    var zDiffs = subZs - cameraPosition.z;

                    xDiffs = xDiffs * xDiffs;
                    yDiffs = yDiffs * yDiffs;
                    zDiffs = zDiffs * zDiffs;

                    var distancesSq = xDiffs + yDiffs + zDiffs;
                    var distances = math.sqrt(distancesSq);
                    cellsDistances.ReinterpretStore(i, distances);

                    var distancesMask = distances < subSpawnDistances;
                    distanceCellsVisibility.SetBits((int)i, (ulong)math.bitmask(distancesMask), 4);

                    // === Merge
                    outputCellsVisibility.SetBits((int)i, (ulong)math.bitmask(partialVisibleMask & distancesMask), 4);
                }

                // In case the number of entities isn't neatly divisible by 4, cull the last few spheres individually
                for (uint i = radii.Length.SimdTrailing(); i < radii.Length; ++i) {
                    // === Frustum
                    var pos = new float3(xs[i], ys[i], zs[i]);
                    var radius = radii[i];

                    var dotPlane0 = math.dot(p0.xyz, pos) + p0.w;
                    var dotPlane1 = math.dot(p1.xyz, pos) + p1.w;
                    var dotPlane2 = math.dot(p2.xyz, pos) + p2.w;
                    var dotPlane3 = math.dot(p3.xyz, pos) + p3.w;
                    var dotPlane4 = math.dot(p4.xyz, pos) + p4.w;
                    var dotPlane5 = math.dot(p5.xyz, pos) + p5.w;

                    bool fullVisibility = dotPlane0 - radius > 0.0f &&
                                          dotPlane1 - radius > 0.0f &&
                                          dotPlane2 - radius > 0.0f &&
                                          dotPlane3 - radius > 0.0f &&
                                          dotPlane4 - radius > 0.0f &&
                                          dotPlane5 - radius > 0.0f;

                    bool partialVisibility = dotPlane0 + radius > 0.0f &&
                                             dotPlane1 + radius > 0.0f &&
                                             dotPlane2 + radius > 0.0f &&
                                             dotPlane3 + radius > 0.0f &&
                                             dotPlane4 + radius > 0.0f &&
                                             dotPlane5 + radius > 0.0f;

                    frustumPartialCellsVisibility.Set((int)i, partialVisibility);
                    frustumFullCellsVisibility.Set((int)i, fullVisibility);

                    // === Distance
                    var distance = math.distance(pos, cameraPosition);
                    cellsDistances[i] = distance;

                    var distanceVisible = distance < spawnDistances[i];
                    distanceCellsVisibility.Set((int)i, distanceVisible);

                    // === Merge
                    outputCellsVisibility.Set((int)i, partialVisibility && distanceVisible);
                }
            }
        }

        // === Memory Info
        [SuppressMessage("ReSharper", "MissingIndent"), SuppressMessage("ReSharper", "WrongIndentSize"), SuppressMessage("ReSharper", "MissingLinebreak")]
        public unsafe int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 1 + // Cell catalog
                                1 + // Cell persistent data
                                1 + // Calculation outputs
                                1; // Rendering cell data

            ownPlace.Span[0] = new MemorySnapshot("LeshyCells", 0, 0, memoryBuffer[..childrenCount]);

            var childrenSpan = memoryBuffer[..childrenCount].Span;
            // Cell catalog
            var catalogSize = (ulong)(cellsCatalog.Length * sizeof(CatalogCellData));
            childrenSpan[0] = new MemorySnapshot("Cell catalog", catalogSize, default);
            // Cell persistent data
            var cellPersistentSize = xs.Length * (
                    sizeof(float) + sizeof(float) + sizeof(float) + // Position
                    sizeof(float) + sizeof(float) + // Radii
                    sizeof(float) + // spawnDistancesSq
                    sizeof(float4x2) + sizeof(float4x2) + // lodDistances
                    sizeof(uint) + // layers
                    sizeof(float) + sizeof(float) + sizeof(float) // AABB center
                    );
            childrenSpan[1] = new MemorySnapshot("Cell persistent data", (ulong)cellPersistentSize, (ulong)cellPersistentSize);
            // Cell calculation outputs
            var cellCalculationOutputsSize =
                cellsDistances.Length * sizeof(float) +
                frustumPartialCellsVisibility.SafeCapacity() * 4 / 8 + // bits to bytes
                possibleLods.Length * sizeof(byte);
            childrenSpan[2] = new MemorySnapshot("Cell calculation outputs", (ulong)cellCalculationOutputsSize, (ulong)cellCalculationOutputsSize);
            // Rendering cell data
            long renderingCellDataSize = 0;
            for (uint i = 0; i < cellsInstances.Length; i++) {
                var cellInstances = cellsInstances[i];
                if (!cellInstances.IsCreated) {
                    continue;
                }
                renderingCellDataSize += cellInstances.instanceVisibilities.Capacity / 8; // bits to bytes
                renderingCellDataSize += cellInstances.instancesSelectedLod.Length * sizeof(byte);
                renderingCellDataSize += cellInstances.rangeIds.Length * sizeof(RangeId);
            }
            for (uint i = 0; i < perInstanceVisibility.Length; i++) {
                var visibilities = perInstanceVisibility[i];
                if (!visibilities.IsCreated) {
                    continue;
                }
                renderingCellDataSize += visibilities.Capacity / 8; // bits to bytes
            }
            childrenSpan[3] = new MemorySnapshot("Rendering cell data", (ulong)renderingCellDataSize, (ulong)renderingCellDataSize);

            return childrenCount;
        }
    }
}
