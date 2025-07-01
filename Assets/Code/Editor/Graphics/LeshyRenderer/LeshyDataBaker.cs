using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.LeshyRenderer;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using AwesomeTechnologies.VegetationSystem;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.LeshyRenderer {
    public static unsafe class LeshyDataBaker {
        // public static void TransformBakedVegetation(LeshyPrefabs target,
        //     VegetationSystemPro vegetationSystemPro, PersistentVegetationStorage persistentVegetationStorage, List<LeshyObjectSettings> handPlacedInstances) {
        //     try {
        //         TransformBakedVegetationImpl(target, vegetationSystemPro, persistentVegetationStorage, handPlacedInstances);
        //     } finally {
        //         EditorUtility.ClearProgressBar();
        //     }
        //
        //     AssetDatabase.Refresh();
        // }

        // static void TransformBakedVegetationImpl(LeshyPrefabs leshyPrefabs,
        //     VegetationSystemPro vegetationSystemPro, PersistentVegetationStorage persistentVegetationStorage, List<LeshyObjectSettings> handPlacedInstances) {
        //     var vspCellSize = vegetationSystemPro.VegetationCellSize;
        //     var vspBounds = vegetationSystemPro.VegetationSystemBounds.ToAABB();
        //     var vspSceneName = vegetationSystemPro.gameObject.scene.name;
        //     // Load VSP data
        //     var vspGridSize = new int2((int)math.ceil(vspBounds.Size.x / vspCellSize),
        //         (int)math.ceil(vspBounds.Size.z / vspCellSize));
        //     var vspCellsCount = vspGridSize.x * vspGridSize.y;
        //
        //     EditorUtility.DisplayProgressBar("Leshy", $"Calculate vsp count", 0);
        //
        //     var vspRemap = new Dictionary<string, int>();
        //     var vspRemapsList = leshyPrefabs.VspRemaps;
        //     for (int i = 0; i < vspRemapsList.Length; i++) {
        //         if (!vspRemap.TryAdd(vspRemapsList[i].vspId, vspRemapsList[i].prefabIndex)) {
        //             Log.Critical?.Error($"Duplicate VSP item with id: {vspRemapsList[i].vspId}");
        //         }
        //     }
        //
        //     var handPlacedInstanceIndexToPrefabIndexMap = new Dictionary<int, int>();
        //     var handPlacedPrefabIndexToInstanceCountMap = new Dictionary<int, int>();
        //     for (int i = 0; i < leshyPrefabs.HandPlacedRemaps.Length; i++) {
        //         var prefabIndex = leshyPrefabs.HandPlacedRemaps[i].prefabIndex;
        //         var instancesCount = handPlacedPrefabIndexToInstanceCountMap.GetValueOrDefault(prefabIndex, 0);
        //         handPlacedPrefabIndexToInstanceCountMap[prefabIndex] = ++instancesCount;
        //         if (!handPlacedInstanceIndexToPrefabIndexMap.TryAdd(leshyPrefabs.HandPlacedRemaps[i].instanceIndex, prefabIndex)) {
        //             Log.Critical?.Error($"Duplicate hand placed vegetation item with id: {leshyPrefabs.HandPlacedRemaps[i].instanceIndex}");
        //         }
        //     }
        //     
        //     var prefabs = leshyPrefabs.Prefabs;
        //     var allHandPlacedInstancesMatrices = LoadHandPlacedMatrices(handPlacedInstances, handPlacedInstanceIndexToPrefabIndexMap,
        //         handPlacedPrefabIndexToInstanceCountMap.ToArray(), out int handPlacedPrefabsMinIndex);
        //     var vspPrefabsCount = handPlacedPrefabsMinIndex == -1 ? prefabs.Length : handPlacedPrefabsMinIndex;
        //     var handPlacedPrefabsCount = handPlacedPrefabsMinIndex == -1 ? 0 : prefabs.Length - handPlacedPrefabsMinIndex;
        //     var vspMatricesCount = CalculateMatricesCount(persistentVegetationStorage, vspCellsCount, vspRemap, vspPrefabsCount);
        //     var allVspMatrices = LoadAllVspMatrices(persistentVegetationStorage, vspMatricesCount, vspCellsCount, vspRemap);
        //     
        //     var loadedCount = allVspMatrices.Sum(static m => m.Length);
        //     Log.Important?.Info($"All loaded {loadedCount}; count {vspMatricesCount.Sum()}");
        //     for (int prefabIndex = 0; prefabIndex < allVspMatrices.Length; prefabIndex++) {
        //         NativeList<MatrixInstance> matrixInstances = allVspMatrices[prefabIndex];
        //         Log.Important?.Info($"For prefab {prefabIndex}: {matrixInstances.Length} vs {vspMatricesCount[prefabIndex]}");
        //     }
        //
        //     var minCellSize = prefabs.Min(static p => p.cellSize);
        //     var boundsBottomLeft = vspBounds.Min;
        //     var maxCellsCount = (int)math.ceil(vspBounds.Size.x / minCellSize) *
        //                         (int)math.ceil(vspBounds.Size.z / minCellSize) *
        //                         prefabs.Length;
        //
        //     var basePath = LeshyPersistence.BasePath(vspSceneName);
        //     var matricesPath = Path.Combine(basePath, LeshyPersistence.MatricesBinFile);
        //
        //     Directory.CreateDirectory(basePath);
        //     var matricesFileStream = new FileStream(matricesPath, FileMode.Create);
        //
        //     var catalogCells = new NativeList<CatalogCellData>(maxCellsCount, Allocator.TempJob);
        //     uint matricesOffset = 0;
        //
        //     var savedCount = 0;
        //     for (var prefabIndex = 0; prefabIndex < vspPrefabsCount; prefabIndex++) {
        //         var matrices = allVspMatrices[prefabIndex];
        //         SavePrefabCellMatrices(prefabIndex, prefabs[prefabIndex].cellSize, vspBounds.Size.xz,
        //             boundsBottomLeft, matrices, ref matricesOffset, ref savedCount,
        //             catalogCells, matricesFileStream);
        //     }
        //
        //     if (handPlacedPrefabsCount != 0) {
        //         var handPlacesPrefabsRangeEnd = handPlacedPrefabsMinIndex + handPlacedPrefabsCount;
        //         for (int prefabIndex = handPlacedPrefabsMinIndex; prefabIndex < handPlacesPrefabsRangeEnd; prefabIndex++) {
        //             var matrices = allHandPlacedInstancesMatrices[prefabIndex - handPlacedPrefabsMinIndex];
        //             SavePrefabCellMatrices(prefabIndex, prefabs[prefabIndex].cellSize, vspBounds.Size.xz,
        //                 boundsBottomLeft, matrices, ref matricesOffset, ref savedCount,
        //                 catalogCells, matricesFileStream);
        //         }
        //     }
        //     
        //
        //     Log.Important?.Info($"Saved {savedCount} inside {catalogCells.Length} cells");
        //     Log.Important?.Info($"Left {allVspMatrices.Sum(static m => m.Length)} items");
        //
        //     EditorUtility.DisplayProgressBar("Leshy", "Saving", 0.9f);
        //     matricesFileStream.Dispose();
        //
        //     var fileInfo = new FileInfoResult();
        //     AsyncReadManager.GetFileInfo(matricesPath, &fileInfo).JobHandle.Complete();
        //     Log.Important?.Info($"File size {fileInfo.FileSize}, saved bytes: {matricesOffset}");
        //
        //     var catalogPath = Path.Combine(basePath, LeshyPersistence.CellsCatalogBinFile);
        //     var catalogStream = new FileStream(catalogPath, FileMode.Create);
        //     catalogStream.Write(catalogCells.AsByteSpan());
        //     catalogStream.Dispose();
        //
        //     // -- Dispose
        //     vspMatricesCount.Dispose();
        //
        //     for (int i = 0; i < allVspMatrices.Length; i++) {
        //         allVspMatrices[i].Dispose();
        //     }
        //
        //     catalogCells.Dispose();
        // }

        // static void SavePrefabCellMatrices(int prefabIndex, float prefabCellSize, float2 boundsSize, float3 boundsBottomLeft,
        //     NativeList<MatrixInstance> prefabInstancesMatrices, ref uint matricesOffset, ref int savedCount,
        //     NativeList<CatalogCellData> catalogCells, FileStream matricesFileStream) {
        //     
        //     var gridSize = new int2((int)math.ceil(boundsSize.x / prefabCellSize),
        //         (int)math.ceil(boundsSize.y / prefabCellSize));
        //
        //     var allMatrices = prefabInstancesMatrices;
        //
        //     for (var z = 0; z < gridSize.y; z++) {
        //         for (int x = 0; x < gridSize.x; x++) {
        //             EditorUtility.DisplayProgressBar("Leshy", $"Creating new cell {prefabIndex} {x};{z}", 0.3f);
        //
        //             var cellBounds = new MinMaxAABB {
        //                 Min = boundsBottomLeft + new float3(x * prefabCellSize, 0, z * prefabCellSize),
        //                 Max = boundsBottomLeft + new float3((x + 1) * prefabCellSize, 0, (z + 1) * prefabCellSize),
        //             };
        //             var outBounds = new NativeReference<MinMaxAABB>(cellBounds, Allocator.TempJob);
        //             var cellMatrices = new NativeList<SmallTransform>(5_000, Allocator.TempJob);
        //
        //             new LoadDataIntoCell {
        //                 cellBounds = cellBounds,
        //                 matrices = allMatrices,
        //                 cellMatrices = cellMatrices,
        //                 outputBounds = outBounds,
        //             }.Run();
        //
        //             if (cellMatrices.Length > 0) {
        //                 catalogCells.Add(new CatalogCellData {
        //                     bounds = outBounds.Value,
        //                     prefabId = (ushort)prefabIndex,
        //                     instancesCount = (uint)cellMatrices.Length,
        //                     matricesOffset = matricesOffset,
        //                 });
        //
        //                 Log.Important?.Info($"Saved for {prefabIndex}: {cellMatrices.Length} instances", logOption: LogOption.NoStacktrace);
        //                 savedCount += cellMatrices.Length;
        //
        //                 var byteSpan = cellMatrices.AsByteSpan();
        //                 matricesFileStream.Write(byteSpan);
        //
        //                 matricesOffset += (uint)byteSpan.Length;
        //             }
        //
        //             outBounds.Dispose();
        //             cellMatrices.Dispose();
        //         }
        //     }
        // }

        // static NativeArray<int> CalculateMatricesCount(PersistentVegetationStorage persistentVegetationStorage,
        //     int vspCellsCount, Dictionary<string, int> vspRemap, int prefabsCount) {
        //     var vspMatricesCount = new NativeArray<int>(prefabsCount, Allocator.TempJob);
        //     for (var i = 0; i < vspCellsCount; i++) {
        //         var vspCell = persistentVegetationStorage.GetCell(i);
        //         vspCell.Init();
        //         for (int j = 0; j < vspCell.Items.Count; j++) {
        //             PersistentVegetationCellItem cellItem = vspCell.Items[j];
        //             var vspId = cellItem.VegetationItemID;
        //             var id = vspRemap[vspId];
        //             vspMatricesCount[id] += cellItem.InstanceCount;
        //         }
        //     }
        //     return vspMatricesCount;
        // }

        // static NativeList<MatrixInstance>[] LoadHandPlacedMatrices(List<LeshyObjectSettings> handPlacedInstances,
        //     Dictionary<int, int> handPlacedInstanceIndexToPrefabIndexMap, KeyValuePair<int, int>[] handPlacedPrefabIndexToInstanceCountMapArr,
        //     out int handPlacedPrefabsMinIndex) {
        //     int prefabsCount = handPlacedPrefabIndexToInstanceCountMapArr.Length;
        //     var allMatrices = new NativeList<MatrixInstance>[prefabsCount];
        //     if (prefabsCount == 0) {
        //         handPlacedPrefabsMinIndex = -1;
        //         return allMatrices;
        //     }
        //     // Hand placed prefabs are added after vsp prefabs, so their indices start not from 0. 
        //     // Therefore, it is needed to find out, which prefab index is minimal to offset prefabIndex 
        //     // returned from map dictionary to get [0-length) index
        //     handPlacedPrefabsMinIndex = int.MaxValue;
        //     for (int i = 0; i < prefabsCount; i++) {
        //         (int prefabIndex, int prefabInstancesCount) = handPlacedPrefabIndexToInstanceCountMapArr[i];
        //         if (prefabIndex < handPlacedPrefabsMinIndex) {
        //             handPlacedPrefabsMinIndex = prefabIndex;
        //         }
        //         allMatrices[i] = new NativeList<MatrixInstance>(prefabInstancesCount, Allocator.TempJob);
        //     }
        //     int allInstancesCount = handPlacedInstances.Count;
        //     for (int instanceIndex = 0; instanceIndex < allInstancesCount; instanceIndex++) {
        //         if (handPlacedInstanceIndexToPrefabIndexMap.TryGetValue(instanceIndex, out var prefabIndex) == false) {
        //             continue;
        //         }
        //         var instanceSettings = handPlacedInstances[instanceIndex];
        //         var instanceTransform = instanceSettings.transform;
        //         instanceTransform.GetPositionAndRotation(out var position, out var rotation);
        //         var scale = (float3)instanceTransform.lossyScale;
        //         MatrixInstance matrixInstance = new MatrixInstance(
        //             position, (quaternionHalf)rotation, (half3)scale, (half)instanceSettings.distanceFalloff);
        //         var prefabIndexWithOffset = prefabIndex - handPlacedPrefabsMinIndex;
        //         allMatrices[prefabIndexWithOffset].Add(matrixInstance);
        //     }
        //     return allMatrices;
        // }
        // static NativeList<MatrixInstance>[] LoadAllVspMatrices(PersistentVegetationStorage persistentVegetationStorage,
        //     NativeArray<int> vspMatricesCount, int vspCellsCount, Dictionary<string, int> vspRemap) {
        //     var allMatrices = new NativeList<MatrixInstance>[vspMatricesCount.Length];
        //     for (int i = 0; i < allMatrices.Length; i++) {
        //         allMatrices[i] = new NativeList<MatrixInstance>(vspMatricesCount[i], Allocator.TempJob);
        //     }
        //
        //     var offsets = new int[vspMatricesCount.Length];
        //
        //     var vspCellMatrices = new List<NativeList<MatrixInstance>>(1);
        //     var matrixInstanceSize = UnsafeUtility.SizeOf<MatrixInstance>();
        //     vspCellMatrices.Add(new NativeList<MatrixInstance>(512, Allocator.TempJob));
        //     var idList = new List<string>(1);
        //     for (var i = 0; i < vspCellsCount; i++) {
        //         EditorUtility.DisplayProgressBar("Leshy", $"Loading VSP cell {i} / {vspCellsCount}", 0.1f);
        //         var vspCell = persistentVegetationStorage.GetCell(i);
        //         vspCell.Init();
        //         idList.Clear();
        //         idList.Add(string.Empty);
        //
        //         for (int j = 0; j < vspCell.Items.Count; j++) {
        //             PersistentVegetationCellItem cellItem = vspCell.Items[j];
        //             if (cellItem.InstanceCount < 1) {
        //                 continue;
        //             }
        //
        //             var vspId = cellItem.VegetationItemID;
        //             idList[0] = vspId;
        //             var id = vspRemap[vspId];
        //
        //             vspCell.LoadCellData(vspCellMatrices, idList, PersistentVegetationCell.MatricesFileName).Complete();
        //
        //             var allMatricesTarget = allMatrices[id];
        //             var offset = offsets[id];
        //             var targetPtr = allMatricesTarget.GetUnsafePtr() + offset;
        //             UnsafeUtility.MemCpy(targetPtr, vspCellMatrices[0].GetUnsafePtr(), vspCellMatrices[0].Length * matrixInstanceSize);
        //             // Resize just adjust the length, we are using this list as array so we set length to the end of the array
        //             allMatricesTarget.Resize(allMatricesTarget.Length + vspCellMatrices[0].Length, NativeArrayOptions.UninitializedMemory);
        //             offsets[id] = offset + vspCellMatrices[0].Length;
        //         }
        //     }
        //
        //     vspCellMatrices[0].Dispose();
        //     vspCellMatrices.Clear();
        //     return allMatrices;
        // }

        // [BurstCompile]
        // struct LoadDataIntoCell : IJob {
        //     public MinMaxAABB cellBounds;
        //     public NativeList<MatrixInstance> matrices;
        //
        //     [WriteOnly] public NativeList<SmallTransform> cellMatrices;
        //     [WriteOnly] public NativeReference<MinMaxAABB> outputBounds;
        //
        //     public void Execute() {
        //         float2 minMaxHeight = new float2(float.PositiveInfinity, float.NegativeInfinity);
        //         var heightExtendedBounds = new MinMaxAABB {
        //             Min = new float3(cellBounds.Min.x, -5_000, cellBounds.Min.z),
        //             Max = new float3(cellBounds.Max.x, 5_000, cellBounds.Max.z),
        //         };
        //
        //         for (var i = matrices.Length - 1; i >= 0; i--) {
        //             var point = matrices[i].Position;
        //             if (heightExtendedBounds.Contains(point)) {
        //                 minMaxHeight.x = math.min(minMaxHeight.x, point.y);
        //                 minMaxHeight.y = math.max(minMaxHeight.y, point.y);
        //                 cellMatrices.Add(new SmallTransform(matrices[i].Position, matrices[i].Rotation, matrices[i].Scale));
        //
        //                 matrices.RemoveAtSwapBack(i);
        //             }
        //         }
        //
        //         outputBounds.Value = new MinMaxAABB {
        //             Min = new float3(cellBounds.Min.x, minMaxHeight.x, cellBounds.Min.z),
        //             Max = new float3(cellBounds.Max.x, minMaxHeight.y, cellBounds.Max.z),
        //         };
        //     }
        // }
    }
}