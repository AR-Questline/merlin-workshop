using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniversalProfiling;
using Object = UnityEngine.Object;

namespace Awaken.TG.LeshyRenderer {
    public struct LeshyCollidersManager {
        const int CollidersPreallocation = 0;
        const int ExpandingSpawnCount = 1;

        static readonly UniversalProfilerMarker InitMarker = new("Leshy.Init");
        static readonly UniversalProfilerMarker UpdateCollidersMarker = new("Leshy.UpdateColliders");
        static readonly UniversalProfilerMarker PlaceRequiredCollidersMarker = new("Leshy.PlaceRequiredColliders");
        static readonly UniversalProfilerMarker AddCollidersMarker = new("Leshy.AddColliders");
        static readonly UniversalProfilerMarker RemoveDespawnedMarker = new("Leshy.RemoveDespawned");
        static readonly UniversalProfilerMarker PreparePossibleMarker = new("Leshy.PreparePossible");

        UnsafeBitmask _hasColliderByCell;
        UnsafeArray<float> _colliderDistancesSqByCell;
        
        UnsafeArray<UnsafeBitmask> _collidersToHaveByCell;
        UnsafeArray<uint> _cellsWithColliderData;

        Scene _scene;
        StructList<ColliderData>[] _collidersByPrefab;

        public bool Enabled { get; set; }

        public void Init(Scene scene, in LeshyCells cells, LeshyPrefabs prefabs) {
            InitMarker.Begin();
            _scene = scene;
            Enabled = true;
            var cellsCount = (uint)cells.CellsCount;
            _hasColliderByCell = new UnsafeBitmask(cellsCount, Allocator.Persistent);
            _colliderDistancesSqByCell = new UnsafeArray<float>(cellsCount, Allocator.Persistent);
            _collidersToHaveByCell = new UnsafeArray<UnsafeBitmask>(cellsCount, Allocator.Persistent);
            
            for (uint i = 0; i < cells.CellsCount; i++) {
                var cell = cells.cellsCatalog[i];
                var prefab = prefabs.Prefabs[cell.prefabId];
                _hasColliderByCell[i] = prefab.HasCollider;
                _colliderDistancesSqByCell[i] = math.square(prefab.colliderDistance);
            }

            _hasColliderByCell.ToIndicesOfOneArray(Allocator.Persistent, out _cellsWithColliderData);
            
            _collidersByPrefab = new StructList<ColliderData>[prefabs.Prefabs.Length];
            for (int i = 0; i < prefabs.Prefabs.Length; i++) {
                ref readonly var prefab = ref prefabs.Prefabs[i];
                if (prefab.HasCollider) {
                    var colliders = new StructList<ColliderData>(CollidersPreallocation);
                    AddColliders(prefab, CollidersPreallocation, ref colliders);
                    _collidersByPrefab[i] = colliders;
                }
            }
            InitMarker.End();
        }

        public void Dispose() {
            _hasColliderByCell.Dispose();
            _colliderDistancesSqByCell.Dispose();
            for (uint i = 0; i < _collidersToHaveByCell.Length; i++) {
                ref var collidersToHave = ref _collidersToHaveByCell[i];
                if (collidersToHave.IsCreated) {
                    collidersToHave.Dispose();
                }
            }
            _collidersToHaveByCell.Dispose();
            _cellsWithColliderData.Dispose();

            foreach (var colliders in _collidersByPrefab) {
                foreach (var collider in colliders) {
                    if (collider.gameObject != null) {
                        GameObjects.DestroySafely(collider.gameObject);
                    }
                }
            }

            _collidersByPrefab = default;
        }

        public void UpdateColliders(float3 cameraPosition, UnsafeBitmask spawnedCells,
            in LeshyCells cells, in LeshyLoadingManager loadingManager, LeshyPrefabs prefabs) {
            if (!Enabled) {
                return;
            }
            UpdateCollidersMarker.Begin();

            RemoveDespawnedMarker.Begin();
            for (uint i = 0u; i < _cellsWithColliderData.Length; i++) {
                var cellIndex = _cellsWithColliderData[i];
                ref var collidersToHave = ref _collidersToHaveByCell[cellIndex];
                if (collidersToHave.IsCreated && spawnedCells[cellIndex] == false) {
                    collidersToHave.Dispose();
                }
            }
            RemoveDespawnedMarker.End();

            PreparePossibleMarker.Begin();
            var possibleCells = new NativeList<int>(256, Allocator.TempJob);
            spawnedCells.ToIndicesOfOneArray(ARAlloc.TempJob, out var spawnedCellIndices);

            var filterPossibleCellsJob = new FilterForPossibleCellsJob {
                spawnedCells = spawnedCellIndices,
                hasCollider = _hasColliderByCell,

                cellsDistances = cells.cellsDistances,
                cellsRadii = cells.cellsRadii,
                colliderDistancesSq = _colliderDistancesSqByCell,
            };
            filterPossibleCellsJob.RunAppendByRef(possibleCells, spawnedCellIndices.LengthInt);

            var filteredData = loadingManager.filteredData;
            var allocateJob = new AllocateCollidersToHaveJob {
                possibleCells = possibleCells,
                spawnedCellIndices = spawnedCellIndices,
                filteredData = filteredData,
                collidersToHaveByCell = _collidersToHaveByCell,
            };
            allocateJob.RunByRef();

            var calculateCollidersJob = new CalculateCollidersToHaveJob {
                cameraPosition = cameraPosition,
                spawnedCellIndices = spawnedCellIndices,
                possibleCells = possibleCells.AsArray(),

                cellsTransforms = filteredData,
                colliderDistancesSqByCell = _colliderDistancesSqByCell,

                collidersToHaveByCell = _collidersToHaveByCell,
            };
            calculateCollidersJob.RunByRef(possibleCells.Length);
            
            PreparePossibleMarker.End();

            PlaceRequiredCollidersMarker.Begin();
            var prefabIterators = new UnsafeArray<int>((uint)_collidersByPrefab.Length, Allocator.TempJob);
            for (int i = 0; i < possibleCells.Length; i++) {
                var cellIndex = spawnedCellIndices[(uint)possibleCells[i]];
                var transforms = filteredData[cellIndex];
                
                var prefabIndex = cells.cellsCatalog[cellIndex].prefabId;
                ref var prefabIterator = ref prefabIterators[prefabIndex];
                ref var colliders = ref _collidersByPrefab[prefabIndex];
                
                foreach (var colliderIndex in _collidersToHaveByCell[cellIndex].EnumerateOnes()) {
                    if (prefabIterator >= colliders.Count) {
                        AddColliders(prefabs.Prefabs[prefabIndex], ExpandingSpawnCount, ref colliders);
                    }
                    
                    var matrix = transforms[colliderIndex];
                    var position = matrix.position;
                    var rotation = matrix.rotation;
                    var scale = matrix.scale;
                    
                    ref readonly var collider = ref colliders[prefabIterator++];
                    collider.gameObject.SetActive(true);
                    collider.transform.SetLocalPositionAndRotation(position, rotation);
                    collider.transform.localScale = (float3)scale;
                }
            }
            for (uint i = 0; i < _collidersByPrefab.Length; i++) {
                ref readonly var colliders = ref _collidersByPrefab[i];
                for (int j = prefabIterators[i]; j < colliders.Count; j++) {
                    colliders[j].gameObject.SetActive(false);
                }
            }
            PlaceRequiredCollidersMarker.End();
            
            prefabIterators.Dispose();
            spawnedCellIndices.Dispose();
            possibleCells.Dispose();
            
            UpdateCollidersMarker.End();
        }

        void AddColliders(in LeshyPrefabs.PrefabAuthoring prefab, int count, ref StructList<ColliderData> colliders) {
            AddCollidersMarker.Begin();
            for (int i = 0; i < count; i++) {
                var collider = (GameObject) Object.Instantiate(prefab.colliders, _scene);
#if UNITY_EDITOR
                collider.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
#endif
                colliders.Add(new ColliderData(collider));
            }
            AddCollidersMarker.End();
        }

        [BurstCompile]
        struct FilterForPossibleCellsJob : IJobFilter {
            [ReadOnly] public UnsafeArray<uint> spawnedCells;
            [ReadOnly] public UnsafeBitmask hasCollider;

            [ReadOnly] public UnsafeArray<float> cellsDistances;
            [ReadOnly] public UnsafeArray<float> cellsRadii;
            [ReadOnly] public UnsafeArray<float> colliderDistancesSq;

            public bool Execute(int index) {
                var cellIndex = spawnedCells[(uint)index];
                if (!hasCollider[cellIndex]) {
                    return false;
                }
                var minDistance = cellsDistances[cellIndex] - cellsRadii[cellIndex];
                return (minDistance < 0) | (math.square(minDistance) <= colliderDistancesSq[cellIndex]);
            }
        }

        [BurstCompile]
        struct AllocateCollidersToHaveJob : IJob {
            [ReadOnly] public NativeList<int> possibleCells;
            [ReadOnly] public UnsafeArray<uint> spawnedCellIndices;
            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> filteredData;

            public UnsafeArray<UnsafeBitmask> collidersToHaveByCell;

            public void Execute() {
                foreach (var cellIndexCell in possibleCells) {
                    var cellIndex = spawnedCellIndices[(uint)cellIndexCell];
                    ref var collidersToHave = ref collidersToHaveByCell[cellIndex];
                    if (collidersToHave.IsCreated) {
                        continue;
                    }
                    var transformsLength = filteredData[cellIndex].Length;
                    collidersToHave = new UnsafeBitmask(transformsLength, ARAlloc.Persistent);
                }
            }
        }

        [BurstCompile]
        struct CalculateCollidersToHaveJob : IJobFor {
            public float3 cameraPosition;
            [ReadOnly] public UnsafeArray<uint> spawnedCellIndices;
            [ReadOnly] public NativeArray<int> possibleCells;

            [ReadOnly] public UnsafeArray<UnsafeArray<SmallTransform>.Span> cellsTransforms;
            [ReadOnly] public UnsafeArray<float> colliderDistancesSqByCell;

            [WriteOnly] public UnsafeArray<UnsafeBitmask> collidersToHaveByCell;

            public void Execute(int index) {
                var cellIndex = spawnedCellIndices[(uint)possibleCells[index]];

                var matrices = cellsTransforms[cellIndex];
                var colliderDistanceSq = colliderDistancesSqByCell[cellIndex];
                ref var collidersToHave = ref collidersToHaveByCell[cellIndex];

                for (uint i = 0; matrices.Length - i >= 4; i += 4) {
                    var t1 = matrices[i+0];
                    var t2 = matrices[i+1];
                    var t3 = matrices[i+2];
                    var t4 = matrices[i+3];

                    var subXs = new float4(t1.position.x, t2.position.x, t3.position.x, t4.position.x);
                    var subYs = new float4(t1.position.y, t2.position.y, t3.position.y, t4.position.y);
                    var subZs = new float4(t1.position.z, t2.position.z, t3.position.z, t4.position.z);

                    var xDiffs = subXs - cameraPosition.x;
                    var yDiffs = subYs - cameraPosition.y;
                    var zDiffs = subZs - cameraPosition.z;

                    xDiffs = xDiffs * xDiffs;
                    yDiffs = yDiffs * yDiffs;
                    zDiffs = zDiffs * zDiffs;

                    var distancesSq = xDiffs + yDiffs + zDiffs;
                    
                    var currentMask = distancesSq <= colliderDistanceSq;
                    collidersToHave.StoreSIMD(i, currentMask);
                }

                for (uint i = matrices.Length.SimdTrailing(); i < matrices.Length; ++i) {
                    var matrix = matrices[i];
                    var position = matrix.position;

                    var distanceSq = math.distancesq(position, cameraPosition);
                    
                    var current = distanceSq < colliderDistanceSq;
                    collidersToHave[i] = current;
                }
            }
        }
        
        struct ColliderData {
            public Transform transform;
            public GameObject gameObject;

            public ColliderData(GameObject gameObject) {
                this.transform = gameObject.transform;
                this.gameObject = gameObject;
            }
        }
    }
}
