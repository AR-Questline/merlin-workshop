using UnityEngine;
using System.Collections.Generic;
using Graphs.Utilities;
using Pathfinding.Rasterizing;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace Pathfinding.Graphs.Navmesh {
	using System;
	using Pathfinding;
	using Voxelization.Burst;
	using Pathfinding.Util;
	using Pathfinding.Jobs;
	using Pathfinding.Collections;
	using Pathfinding.Pooling;
	using UnityEngine.Profiling;
	using Unity.Collections.LowLevel.Unsafe;

	[BurstCompile]
	public class RecastMeshGatherer {
		readonly int terrainDownsamplingFactor;
		public readonly LayerMask mask;
		public readonly List<string> tagMask;
		readonly float maxColliderApproximationError;
		public readonly Bounds bounds;
		public readonly UnityEngine.SceneManagement.Scene scene;
		Dictionary<MeshCacheItem, int> cachedMeshes = new Dictionary<MeshCacheItem, int>();
		readonly Dictionary<GameObject, TreeInfo> cachedTreePrefabs = new Dictionary<GameObject, TreeInfo>();
		readonly List<NativeArray<Vector3> > vertexBuffers;
		readonly List<NativeArray<int> > triangleBuffers;
		readonly List<Mesh> meshData;
		readonly RecastGraph.PerLayerModification[] modificationsByLayer;
		readonly RecastGraph.PerLayerModification[] modificationsByLayer2D;
		readonly List<WaterProperties> meshWaterDepth;

#if UNITY_EDITOR
		readonly List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime = new List<(UnityEngine.Object, Mesh)>();
#else
		bool anyNonReadableMesh = false;
#endif

		List<GatheredMesh> meshes;
		List<Material> dummyMaterials = new List<Material>();

		public RecastMeshGatherer (UnityEngine.SceneManagement.Scene scene, Bounds bounds, int terrainDownsamplingFactor, LayerMask mask, List<string> tagMask, List<RecastGraph.PerLayerModification> perLayerModifications, float maxColliderApproximationError)
        {
        }

        struct TreeInfo
        {
            public List<GatheredMesh> submeshes;
            public Vector3 localScale;
            public bool supportsRotation;
        }

        public struct MeshCollection : IArenaDisposable
        {
            List<NativeArray<Vector3>> vertexBuffers;
            List<NativeArray<int>> triangleBuffers;
            public NativeArray<RasterizationMesh> meshes;
#if UNITY_EDITOR
            public List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime;
#endif

            public MeshCollection(List<NativeArray<Vector3>> vertexBuffers, List<NativeArray<int>> triangleBuffers, NativeArray<RasterizationMesh> meshes
#if UNITY_EDITOR
                                   , List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime
#endif
                                   ) : this()
            {
            }

            void IArenaDisposable.DisposeWith(DisposeArena arena)
            {
            }
        }

        [BurstCompile]
        static void CalculateBounds(ref UnsafeSpan<float3> vertices, ref float4x4 localToWorldMatrix, out Bounds bounds)
        {
            bounds = default(Bounds);
        }

        public MeshCollection Finalize()
        {
            return default;
        }

        /// <summary>
        /// Add vertex and triangle buffers that can later be used to create a <see cref="GatheredMesh"/>.
        ///
        /// The returned index can be used in the <see cref="GatheredMesh.meshDataIndex"/> field of the <see cref="GatheredMesh"/> struct.
        ///
        /// You can use the returned index multiple times with different matrices, to create instances of the same object in multiple locations.
        /// </summary>
        public int AddMeshBuffers(Vector3[] vertices, int[] triangles)
        {
            return default;
        }

        /// <summary>
        /// Add vertex and triangle buffers that can later be used to create a <see cref="GatheredMesh"/>.
        ///
        /// The returned index can be used in the <see cref="GatheredMesh.meshDataIndex"/> field of the <see cref="GatheredMesh"/> struct.
        ///
        /// You can use the returned index multiple times with different matrices, to create instances of the same object in multiple locations.
        /// </summary>
        public int AddMeshBuffers(NativeArray<Vector3> vertices, NativeArray<int> triangles)
        {
            return default;
        }

        /// <summary>Add a mesh to the list of meshes to rasterize</summary>
        public void AddMesh(Renderer renderer, Mesh gatheredMesh)
        {
        }

        /// <summary>Add a mesh to the list of meshes to rasterize</summary>
        public void AddMesh(GatheredMesh gatheredMesh)
        {
        }

        /// <summary>Holds info about a mesh to be rasterized</summary>
        public struct GatheredMesh
        {
            /// <summary>
            /// Index in the meshData array.
            /// Can be retrieved from the <see cref="RecastMeshGatherer.AddMeshBuffers"/> method.
            /// </summary>
            public int meshDataIndex;
            /// <summary>
            /// Area ID of the mesh. 0 means walkable, and -1 indicates that the mesh should be treated as unwalkable.
            /// Other positive values indicate a custom area ID which will create a seam in the navmesh.
            /// </summary>
            public int area;
            /// <summary>Start index in the triangle array</summary>
            public int indexStart;
            /// <summary>End index in the triangle array. -1 indicates the end of the array.</summary>
            public int indexEnd;


            /// <summary>World bounds of the mesh. Assumed to already be multiplied with the <see cref="matrix"/>.</summary>
            public Bounds bounds;

            /// <summary>Matrix to transform the vertices by</summary>
            public Matrix4x4 matrix;

            /// <summary>
            /// If true then the mesh will be treated as solid and its interior will be unwalkable.
            /// The unwalkable region will be the minimum to maximum y coordinate in each cell.
            /// </summary>
            public bool solid;
            /// <summary>See <see cref="RasterizationMesh.doubleSided"/></summary>
            public bool doubleSided;
            /// <summary>See <see cref="RasterizationMesh.flatten"/></summary>
            public bool flatten;
            /// <summary>See <see cref="RasterizationMesh.areaIsTag"/></summary>
            public bool areaIsTag;

            /// <summary>
            /// Recalculate the <see cref="bounds"/> from the vertices.
            ///
            /// The bounds will not be recalculated immediately.
            /// </summary>
            public void RecalculateBounds()
            {
            }

            public void ApplyNavmeshModifier(RecastMeshObjStatic meshObjStatic)
            {
            }

            public void ApplyLayerModification(RecastGraph.PerLayerModification modification)
            {
            }
        }

        enum MeshType
        {
            Mesh,
            Box,
            Capsule,
		}

		struct MeshCacheItem : IEquatable<MeshCacheItem> {
			public MeshType type;
			public Mesh mesh;
			public int rows;
            public int quantizedHeight;
            public WaterProperties waterProperties;

            public MeshCacheItem(Mesh mesh, WaterProperties waterProperties) : this()
            {
            }

            public static readonly MeshCacheItem Box = new MeshCacheItem
            {
                type = MeshType.Box,
                mesh = null,
                rows = 0,
                quantizedHeight = 0,
                waterProperties = default
            };

            public bool Equals(MeshCacheItem other)
            {
                return default;
            }

            public override int GetHashCode()
            {
                return default;
            }
        }

        bool MeshFilterShouldBeIncluded(MeshFilter filter)
        {
            return default;
        }

        bool ConvertMeshToGatheredMesh (Renderer renderer, Mesh mesh, WaterProperties waterProperties, out GatheredMesh gatheredMesh)
        {
            gatheredMesh = default(GatheredMesh);
            return default;
        }

        GatheredMesh? GetColliderMesh(MeshCollider collider, Matrix4x4 localToWorldMatrix, WaterProperties waterProperties)
        {
            return default;
        }

        public void CollectSceneMeshes()
        {
        }

        static int AreaFromSurfaceMode(RecastMeshObjStatic.Mode mode, int surfaceID)
        {
            return default;
        }

        /// <summary>Find all relevant RecastNavmeshModifier components and create ExtraMeshes for them</summary>
        public void CollectRecastNavmeshModifiers()
        {
        }

        void AddNavmeshModifier(RecastMeshObjStatic meshObjStatic)
        {
        }

        public void CollectTerrainMeshes(bool rasterizeTrees, float desiredChunkSize)
        {
        }

        static int NonNegativeModulus(int x, int m)
        {
            return default;
        }

        /// <summary>Returns ceil(lhs/rhs), i.e lhs/rhs rounded up</summary>
        static int CeilDivision(int lhs, int rhs)
        {
            return default;
        }

        bool GenerateTerrainChunks(Terrain terrain, Bounds bounds, float desiredChunkSize)
        {
            return default;
        }

        /// <summary>Generates a terrain chunk mesh</summary>
        [BurstCompile]
        public static void GenerateHeightmapChunk(ref UnsafeSpan<float> heights, ref UnsafeSpan<bool> holes, int heightmapWidth, int heightmapDepth, int x0, int z0, int width, int depth, int stride, out UnsafeSpan<Vector3> verts, out UnsafeSpan<int> tris)
        {
            verts = default(UnsafeSpan<Vector3>);
            tris = default(UnsafeSpan<int>);
        }

        void CollectTreeMeshes(Terrain terrain)
        {
        }

        bool ShouldIncludeCollider(Collider collider)
        {
            return default;
        }

        public void CollectColliderMeshes()
        {
        }

        /// <summary>
        /// Box Collider triangle indices can be reused for multiple instances.
        /// Warning: This array should never be changed
        /// </summary>
        private readonly static int[] BoxColliderTris = {
            0, 1, 2,
            0, 2, 3,

            6, 5, 4,
            7, 6, 4,

            0, 5, 1,
            0, 4, 5,

            1, 6, 2,
            1, 5, 6,

            2, 7, 3,
            2, 6, 7,

            3, 4, 0,
            3, 7, 4
        };

        /// <summary>
        /// Box Collider vertices can be reused for multiple instances.
        /// Warning: This array should never be changed
        /// </summary>
        private readonly static Vector3[] BoxColliderVerts = {
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
            new Vector3(1, -1, 1),
            new Vector3(-1, -1, 1),

            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, 1),
        };

        /// <summary>
        /// Rasterizes a collider to a mesh.
        /// This will pass the col.transform.localToWorldMatrix to the other overload of this function.
        /// </summary>
        GatheredMesh? ConvertColliderToGatheredMesh(Collider col, WaterProperties waterProperties)
        {
            return default;
        }

        /// <summary>
        /// Rasterizes a collider to a mesh assuming it's vertices should be multiplied with the matrix.
        /// Note that the bounds of the returned RasterizationMesh is based on collider.bounds. So you might want to
        /// call myExtraMesh.RecalculateBounds on the returned mesh to recalculate it if the collider.bounds would
        /// not give the correct value.
        /// </summary>
        public GatheredMesh? ConvertColliderToGatheredMesh(Collider col, Matrix4x4 localToWorldMatrix, WaterProperties waterProperties)
        {
            return default;
        }

        GatheredMesh RasterizeBoxCollider(BoxCollider collider, Matrix4x4 localToWorldMatrix, WaterProperties waterProperties)
        {
            return default;
        }

        static int CircleSteps(Matrix4x4 matrix, float radius, float maxError)
        {
            return default;
        }

        /// <summary>
        /// If a circle is approximated by fewer segments, it will be slightly smaller than the original circle.
        /// This factor is used to adjust the radius of the circle so that the resulting circle will have roughly the same area as the original circle.
        /// </summary>
        static float CircleRadiusAdjustmentFactor(int steps)
        {
            return default;
        }

        GatheredMesh RasterizeCapsuleCollider (float radius, float height, Bounds bounds, Matrix4x4 localToWorldMatrix) {
            return default;
        }
    }
}
