using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Graphs.Navmesh {
	using Pathfinding;
	using Pathfinding.Util;
	using Pathfinding.Collections;
	using Unity.Collections;
	using Unity.Collections.LowLevel.Unsafe;
	using Unity.Mathematics;
	using Pathfinding.Sync;
	using Pathfinding.Pooling;
	using Unity.Burst;
	using System.Runtime.InteropServices;
	using UnityEngine.Assertions;
	using Unity.Jobs;

#if UNITY_2022_3_OR_NEWER && MODULE_COLLECTIONS_2_2_0_OR_NEWER
	using andywiecko.BurstTriangulator.LowLevel.Unsafe;
#endif

#if MODULE_COLLECTIONS_2_1_0_OR_NEWER
	using NativeHashMapVector2IntInt = Unity.Collections.NativeHashMap<Vector2Int, int>;
	using Unity.Jobs.LowLevel.Unsafe;
#else
	using NativeHashMapVector2IntInt = Unity.Collections.NativeParallelHashMap<Vector2Int, int>;
#endif

	public struct TileCutter {
		NavmeshBase graph;
		GridLookup<NavmeshClipper> cuts;
		TileLayout tileLayout;

		public TileCutter (NavmeshBase graph, GridLookup<NavmeshClipper> cuts, TileLayout tileLayout) : this()
        {
        }

        public struct TileCutterOutput : IProgress, System.IDisposable {
			public TileMeshesUnsafe tileMeshes;

			public float Progress => 0;

			public void Dispose () {
            }
        }

		static void DisposeTileData (UnsafeSpan<UnsafeList<UnsafeSpan<Int3> > > tileVertices, UnsafeSpan<UnsafeList<UnsafeSpan<int> > > tileTriangles, UnsafeSpan<UnsafeList<UnsafeSpan<int> > > tileTags, Allocator allocator, bool skipFirst)
        {
        }

        public static void EnsurePreCutDataExists(NavmeshBase graph, NavmeshTile tile)
        {
        }

        static bool CheckVersion()
        {
            return default;
        }

        public Promise<TileCutterOutput> Schedule(List<Vector2Int> tileCoordinates)
        {
            return default;
        }

        public Promise<TileCutterOutput> Schedule(Promise<TileBuilder.TileBuilderOutput> builderOutput)
        {
            return default;
        }

#if UNITY_2022_3_OR_NEWER && MODULE_COLLECTIONS_2_2_0_OR_NEWER
        [BurstCompile]
        struct JobCutTiles : IJob
        {
            // Will be disposed when the job is done
            public UnsafeSpan<UnsafeList<UnsafeSpan<Int3> > > tileVertices;
			// Will be disposed when the job is done
			public UnsafeSpan<UnsafeList<UnsafeSpan<int> > > tileTriangles;
			// Will be disposed when the job is done
			public UnsafeSpan<UnsafeList<UnsafeSpan<int> > > tileTags;
			// Will be disposed when the job is done
			public TileHandler.CutCollection cutCollection;
			public TileMeshesUnsafe inputTileMeshes;
			public NativeArray<TileMesh.TileMeshUnsafe> outputTileMeshes;

			public void Execute ()
            {
            }
        }
#endif
    }

#if UNITY_2022_3_OR_NEWER && MODULE_COLLECTIONS_2_2_0_OR_NEWER
    static class TileHandlerCache
    {
        
    }
#endif

    /// <summary>
    /// Utility class for updating tiles of navmesh/recast graphs.
    ///
    /// Most operations that this class does are asynchronous.
    /// They will be added as work items to the AstarPath class
    /// and executed when the pathfinding threads have finished
    /// calculating their current paths.
    ///
    /// See: navmeshcutting (view in online documentation for working links)
    /// See: <see cref="NavmeshUpdates"/>
    /// </summary>
    [BurstCompile]
    public static class TileHandler
    {
        static readonly Unity.Profiling.ProfilerMarker MarkerTriangulate = new Unity.Profiling.ProfilerMarker("Triangulate");
        static readonly Unity.Profiling.ProfilerMarker MarkerClipping = new Unity.Profiling.ProfilerMarker("Clipping");
        static readonly Unity.Profiling.ProfilerMarker MarkerPrepare = new Unity.Profiling.ProfilerMarker("Prepare");
        static readonly Unity.Profiling.ProfilerMarker MarkerAllocate = new Unity.Profiling.ProfilerMarker("Allocate");
        static readonly Unity.Profiling.ProfilerMarker MarkerCore = new Unity.Profiling.ProfilerMarker("Core");
        static readonly Unity.Profiling.ProfilerMarker MarkerCompress = new Unity.Profiling.ProfilerMarker("Compress");
        static readonly Unity.Profiling.ProfilerMarker MarkerRefine = new Unity.Profiling.ProfilerMarker("Refine");
        static readonly Unity.Profiling.ProfilerMarker MarkerEdgeSnapping = new Unity.Profiling.ProfilerMarker("EdgeSnapping");
        static readonly Unity.Profiling.ProfilerMarker MarkerCopyClippingResult = new Unity.Profiling.ProfilerMarker("CopyClippingResult");
        static readonly Unity.Profiling.ProfilerMarker CopyTriangulationToOutputMarker = new Unity.Profiling.ProfilerMarker("Copy to output");

#if UNITY_2022_3_OR_NEWER && MODULE_COLLECTIONS_2_2_0_OR_NEWER
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CutFunction(ref UnsafeSpan<Point64Wrapper> subject, ref UnsafeSpan<Point64Wrapper> contourVertices, ref UnsafeSpan<NavmeshCut.ContourBurst> contours, ref UnsafeSpan<int> contourIndices, ref UnsafeSpan<int> contourIndicesDual, ref UnsafeList<Vector2Int> outputVertices, ref UnsafeList<int> outputVertexCountPerPolygon, int dual);

        struct CutFunctionKey { }
        private static readonly SharedStatic<IntPtr> CutFunctionPtr = SharedStatic<IntPtr>.GetOrCreate<CutFunctionKey>();
        private static CutFunction DelegateGCRoot;
#endif

        /// <summary>See <see cref="SnapEdges"/></summary>
        const int EdgeSnappingMaxDistance = 1;

        /// <summary>
        /// See <see cref="ConvertVerticesAndSnapToTileBoundaries"/>.
        ///
        /// The navmesh cut vertices are snapped to tile borders, if they are within this distance to the edge.
        /// This is used to avoid tiiiny slivers of triangles resulting from cuts that are just infringing on a tile.
        /// The normal snapping (using <see cref="EdgeSnappingMaxDistance)"/> cannot be used for tile borders, because that would
        /// make the tile borders not be straight anymore.
        ///
        /// I don't think there's any technical upper limit to this value. It's a tradeoff between the size of the slivers,
        /// and how accurately it matches the original geometry.
        /// </summary>
        public const int TileSnappingMaxDistance = 20;

        internal struct TileCuts
        {
            public int contourStartIndex;
            public int contourEndIndex;
        }

        internal struct ContourMeta
        {
            public bool isDual;
            public bool cutsAddedGeom;
        }

        internal struct CutCollection : System.IDisposable
        {
            /// <summary>
            /// Vertices of all cut contours in all tiles
            /// Stored in tile space for the tile they belong to.
            /// </summary>
            public UnsafeList<Point64Wrapper> contourVertices;
            public UnsafeList<NavmeshCut.ContourBurst> contours;
            public UnsafeList<ContourMeta> contoursExtra;
            public UnsafeList<TileCuts> tileCuts;
            [MarshalAs(UnmanagedType.U1)]
            public bool cuttingRequired;

            public void Dispose()
            {
            }
        }

        // Burst doesn't seem to like referencing types from the Clipper2 dll, so we create
        // a type here that is identical to the Point64 type in Clipper2
        public struct Point64Wrapper
        {
            public long x;
            public long y;

            public Point64Wrapper(long x, long y) : this()
            {
            }
        }

        internal static CutCollection CollectCuts(GridLookup<NavmeshClipper> cuts, List<Vector2Int> tileCoordinates, float characterRadius, TileLayout tileLayout, ref UnsafeSpan<UnsafeList<UnsafeSpan<Int3>>> tileVertices, ref UnsafeSpan<UnsafeList<UnsafeSpan<int>>> tileTriangles, ref UnsafeSpan<UnsafeList<UnsafeSpan<int>>> tileTags)
        {
            return default;
        }

        [BurstCompile]
        static void ConvertVerticesAndSnapToTileBoundaries(ref UnsafeSpan<float2> contourVertices, out UnsafeList<Point64Wrapper> outputVertices, ref Vector2 tileSize)
        {
            outputVertices = default(UnsafeList<Point64Wrapper>);
        }

#if UNITY_2022_3_OR_NEWER && MODULE_COLLECTIONS_2_2_0_OR_NEWER
        [BurstCompile]
        internal static void CutTiles(ref UnsafeSpan<UnsafeList<UnsafeSpan<Int3>>> tileVertices, ref UnsafeSpan<UnsafeList<UnsafeSpan<int>>> tileTriangles, ref UnsafeSpan<UnsafeList<UnsafeSpan<int>>> tileTags, ref Vector2Int tileSize, ref CutCollection cutCollection, ref UnsafeSpan<TileMesh.TileMeshUnsafe> output, Allocator allocator)
        {
        }

        static void FindIntersectingCuts(UnsafeSpan<ContourMeta> contoursMeta, UnsafeSpan<IntBounds> cutBounds, NativeList<int> interestingCuts, NativeList<int> interestingDualCuts, IntBounds triBounds, bool addedGeometry)
        {
        }

        static IntBounds TriangleBounds(Int3 a, Int3 b, Int3 c)
        {
            return default;
        }

        static TileMesh.TileMeshUnsafe CompressAndRefineTile(NativeList<Int3> tileOutputVertices, NativeList<int> tileOutputTriangles, NativeList<int> tileOutputTags, Allocator allocator)
        {
            return default;
        }

        static void CopyTriangulationToOutput(ref OutputData<int2> triangulatorOutput, NativeList<Int3> tileOutputVertices, NativeList<int> tileOutputTriangles, NativeList<int> tileOutputTags, int tag, Int3 a, Int3 b, Int3 c)
        {
        }

        /// <summary>
        /// Find cut vertices that lie exactly on the polygon edges, and insert them into the polygon.
        ///
        /// These vertices will need to be added to the polygon outline before cutting,
        /// to ensure they end up in the final triangulation.
        /// This is because adjacent triangles may have a cut there, and
        /// if this triangle doesn't also get a cut there, connections between
        /// nodes may not be created properly.
        ///
        ///    /|  /\
        ///   / | /  \
        ///  /  |/   /
        /// /___| \ /
        ///
        /// The EdgeSnappingMaxDistance must be at least 1 (possibly only 0.5) for this to handle all edge cases.
        /// If it is larger, it will nicely prevent too small triangles, but it can also cause issues when snapping very
        /// thin triangles so much that they become invalid. So a value of 1 seems best.
        ///
        /// Using this method adds some overhead for cutting, but it is necessary to handle edge cases where
        /// navmesh cuts exactly touch the edges of the triangles.
        /// The overhead seems to be roughly 1% of the total cutting time.
        /// </summary>
        static void SnapEdges(ref NativeArray<Point64Wrapper> triBuffer, ref int vertexCount, UnsafeSpan<NavmeshCut.ContourBurst> contours, ref NativeList<int> interestingCuts, UnsafeSpan<Point64Wrapper> contourVerticesP64, Vector2Int tileSize)
        {
        }

        static NativeArray<IntBounds> CalculateCutBounds(ref CutCollection cutCollection, ref UnsafeList<Point64Wrapper> contourVerticesP64)
        {
            return default;
        }

        [AOT.MonoPInvokeCallback(typeof(CutFunction))]
        static bool CutPolygon(ref UnsafeSpan<Point64Wrapper> subject, ref UnsafeSpan<Point64Wrapper> contourVertices, ref UnsafeSpan<NavmeshCut.ContourBurst> contours, ref UnsafeSpan<int> contourIndices, ref UnsafeSpan<int> contourIndicesDual, ref UnsafeList<Vector2Int> outputVertices, ref UnsafeList<int> outputVertexCountPerPolygon, int mode)
        {
            return default;
        }

        internal static void InitDelegates()
        {
        }

        /// <summary>
        /// Clips the input polygon against a rectangle with one corner at the origin and one at size in XZ space.
        ///
        /// Returns: Number of output vertices
        /// </summary>
        /// <param name="clipIn">Input vertices. Output will also be placed here.</param>
        /// <param name="clipTmp">Temporary vertices. This buffer must be large enough to contain all output vertices.</param>
        /// <param name="size">The clipping rectangle has one corner at the origin and one at this position in XZ space.</param>
        static int ClipAgainstRectangle(UnsafeSpan<Int3> clipIn, UnsafeSpan<Int3> clipTmp, Vector2Int size)
        {
            return default;
        }

        /// <summary>
        /// Refine a mesh using delaunay refinement.
        /// Loops through all pairs of neighbouring triangles and check if it would be better to flip the diagonal joining them
        /// using the delaunay criteria.
        ///
        /// Does not require triangles to be clockwise, triangles will be checked for if they are clockwise and made clockwise if not.
        /// The resulting mesh will have all triangles clockwise.
        ///
        /// See: https://en.wikipedia.org/wiki/Delaunay_triangulation
        /// </summary>
        static int DelaunayRefinement(UnsafeSpan<Int3> verts, UnsafeSpan<int> tris, UnsafeSpan<int> tags, bool delaunay, bool colinear)
        {
            return default;
        }
#endif
    }
}
