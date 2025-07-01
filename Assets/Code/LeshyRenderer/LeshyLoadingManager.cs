using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths.Data;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UniversalProfiling;

namespace Awaken.TG.LeshyRenderer {
    public unsafe struct LeshyLoadingManager : IMemorySnapshotProvider {
        static readonly UniversalProfilerMarker LoadCellDataMarker = new UniversalProfilerMarker("Leshy.LoadCellData");
        static readonly UniversalProfilerMarker UpdateLoadingMarker = new UniversalProfilerMarker("Leshy.UpdateLoading");
        static readonly int TransformSize = UnsafeUtility.SizeOf<SmallTransform>();

        FileHandle _matricesFileHandle;
        ushort _maxOngoingReads;

        NativeList<CellToLoad> _cellsToLoad;
        NativeBitArray _loadingCells;
        UnsafeArray<ReadHandle> _matricesReadHandles;
        NativeBitArray _loadedCells;

        public UnsafeArray<UnsafeArray<SmallTransform>> loadedData;
        public UnsafeArray<UnsafeArray<SmallTransform>.Span> filteredData;

        public bool Enabled { get; set; }

        // === Lifetime
        public void Init(ushort maxOngoingReads, string matricesPath, in LeshyCells cells) {
            Enabled = true;
            _maxOngoingReads = maxOngoingReads;
            _matricesFileHandle = AsyncReadManager.OpenFileAsync(matricesPath);

            _cellsToLoad = new NativeList<CellToLoad>(_maxOngoingReads, Allocator.Persistent);
            var cellsCount = cells.CellsCount;
            _matricesReadHandles = new UnsafeArray<ReadHandle>((uint)cellsCount, Allocator.Persistent);
            loadedData = new UnsafeArray<UnsafeArray<SmallTransform>>((uint)cellsCount, Allocator.Persistent);
            filteredData = new UnsafeArray<UnsafeArray<SmallTransform>.Span>((uint)cellsCount, Allocator.Persistent);
            _loadingCells = new NativeBitArray(cellsCount, Allocator.Persistent);
            _loadedCells = new NativeBitArray(cellsCount, Allocator.Persistent);
        }

        public void Dispose() {
            _cellsToLoad.Dispose();

            for (uint i = 0; i < _matricesReadHandles.Length; i++) {
                ref var data = ref _matricesReadHandles[i];
                if (data.IsValid()) {
                    if (data.Status == ReadStatus.InProgress) {
                        data.Cancel();
                    }
                    data.Dispose();
                }
            }
            _matricesReadHandles.Dispose();
            _matricesFileHandle.Close().Complete();
            for (uint i = 0; i < loadedData.Length; i++) {
                ref var data = ref loadedData[i];
                if (data.IsCreated) {
                    data.Dispose();
                }
            }
            loadedData.Dispose();
            filteredData.Dispose();
            _loadingCells.Dispose();
            _loadedCells.Dispose();
        }

        // === Queries
        public bool IsLoaded(int cellIndex) {
            return _loadedCells.IsSet(cellIndex);
        }

        // === Operations
        public void Update(in LeshyCells cells) {
            if (!Enabled) {
                return;
            }
            UpdateLoadingMarker.Begin();
            ProgressOngoingReads();
            StartNewReads(cells);
            UpdateLoadingMarker.End();
        }

        public void DespawnCell(uint cellIndex) {
            Assert.IsFalse(_loadingCells.IsSet((int)cellIndex));
            Assert.IsTrue(_loadedCells.IsSet((int)cellIndex));

            _loadedCells.Set((int)cellIndex, false);
            ClearCellData(cellIndex);
        }

        void ProgressOngoingReads() {
            for (var i = 0; i < _matricesReadHandles.Length; i++) {
                var readHandle = _matricesReadHandles[(uint)i];
                if (_loadingCells.IsSet(i)) {
                    if (readHandle.Status == ReadStatus.Complete) {
                        readHandle.Dispose();
                        _matricesReadHandles[(uint)i] = default;
                        _loadingCells.Set(i, false);
                        _loadedCells.Set(i, true);
                    } else if (readHandle.Status == ReadStatus.Failed) {
                        readHandle.Dispose();
                        _matricesReadHandles[(uint)i] = default;
                        _loadingCells.Set(i, false);
                        _loadedCells.Set(i, false);
                        ClearCellData((uint)i);
                        Log.Important?.Error($"Failed to load matrices for cell {i}");
                    }
                }
            }
        }

        void ClearCellData(uint cellIndex) {
            ref var data = ref loadedData[cellIndex];
            Assert.IsTrue(data.IsCreated);
            data.Dispose();
            data = default;
            filteredData[cellIndex] = default;
        }

        void StartNewReads(in LeshyCells cells) {
            var ongoingReadsCount = (ushort)_loadingCells.CountBits(0, _loadingCells.Length);
            if (ongoingReadsCount == _maxOngoingReads) {
                return;
            }
            var finalVisibility = cells.finalCellsVisibility;
            new FindCellsToStartLoadingJob {
                maxNewLoads = _maxOngoingReads - ongoingReadsCount,
                cellDistances = cells.cellsDistances,

                finalVisibility = finalVisibility,
                loadingCells = _loadingCells,
                loadedCells = _loadedCells,

                cellToLoad = _cellsToLoad,
            }.Run();

            for (int i = 0; i < _cellsToLoad.Length; i++) {
                LoadCellData(cells, _cellsToLoad[i].cellIndex);
            }
            _cellsToLoad.Length = 0;
        }

        void LoadCellData(in LeshyCells cells, uint cellIndex) {
            LoadCellDataMarker.Begin();
            var cellCatalogData = cells.cellsCatalog[cellIndex];

            var matrices = new UnsafeArray<SmallTransform>(cellCatalogData.instancesCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            loadedData[cellIndex] = matrices;

            var matricesReadCommands = new ReadCommand {
                Offset = cellCatalogData.matricesOffset,
                Size = TransformSize*cellCatalogData.instancesCount,
                Buffer = matrices.Ptr,
            };
            var matricesReadCmdArray = new ReadCommandArray {
                ReadCommands = &matricesReadCommands,
                CommandCount = 1,
            };
            var matricesReadHandle = AsyncReadManager.Read(_matricesFileHandle, matricesReadCmdArray);
            _matricesReadHandles[cellIndex] = matricesReadHandle;

            _loadingCells.Set((int)cellIndex, true);
            LoadCellDataMarker.End();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = (int)loadedData.Length;

            var loadingCellsSize = _loadingCells.SafeCapacity() / 8; // bits to bytes
            var matricesLoadingSize = _matricesReadHandles.Length * sizeof(ReadHandle);
            var loadedCellsSize = _loadedCells.SafeCapacity() / 8; // bits to bytes
            var ownSize = loadingCellsSize + matricesLoadingSize + loadedCellsSize;
            ownPlace.Span[0] = new MemorySnapshot("LeshyLoading", ownSize, ownSize, memoryBuffer[..childrenCount]);

            var transformsSpan = memoryBuffer[..childrenCount].Span;
            for (uint i = 0; i < loadedData.Length; i++) {
                var loadedTransforms = loadedData[i];
                var transformsSize = loadedTransforms.Length * TransformSize;
                transformsSpan[(int)i] = new MemorySnapshot($"Cell{i}", transformsSize, default);
            }

            return childrenCount;
        }

        [BurstCompile]
        struct FindCellsToStartLoadingJob : IJob {
            public int maxNewLoads;
            [ReadOnly] public UnsafeArray<float> cellDistances;

            [ReadOnly] public NativeBitArray finalVisibility;
            [ReadOnly] public NativeBitArray loadingCells;
            [ReadOnly] public NativeBitArray loadedCells;

            public NativeList<CellToLoad> cellToLoad;

            public void Execute() {
                for (uint i = 0; i < finalVisibility.Length; i++) {
                    int ii = (int)i;
                    if (finalVisibility.IsSet(ii) && !loadingCells.IsSet(ii) && !loadedCells.IsSet(ii)) {
                        TryInsertCell(i);
                    }
                }
            }

            void TryInsertCell(uint cellIndex) {
                var indexToInsert = cellToLoad.Length;
                var distanceSq = cellDistances[cellIndex];
                for (int i = 0; i < cellToLoad.Length; i++) {
                    if (distanceSq < cellToLoad[i].distanceSq) {
                        indexToInsert = i;
                        break;
                    }
                }
                if (indexToInsert < maxNewLoads) {
                    if (cellToLoad.Length == maxNewLoads) {
                        cellToLoad.RemoveAtSwapBack(cellToLoad.Length-1);
                    }
                    cellToLoad.InsertRange(indexToInsert, 1);
                    cellToLoad[indexToInsert] = new CellToLoad {
                        cellIndex = cellIndex,
                        distanceSq = distanceSq,
                    };
                }
            }
        }

        struct CellToLoad {
            public uint cellIndex;
            public float distanceSq;
        }
    }
}
