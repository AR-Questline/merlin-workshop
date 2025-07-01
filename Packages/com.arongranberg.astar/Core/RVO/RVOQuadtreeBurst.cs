namespace Pathfinding.RVO {
	using UnityEngine;
	using Pathfinding.ECS.RVO;
	using Unity.Burst;
	using Unity.Jobs;
	using Unity.Mathematics;
	using Unity.Collections;
	using Pathfinding.Drawing;

	/// <summary>
	/// Quadtree for quick nearest neighbour search of rvo agents.
	/// See: Pathfinding.RVO.Simulator
	/// </summary>
	public struct RVOQuadtreeBurst {
		const int LeafSize = 16;
		const int MaxDepth = 10;

		NativeArray<int> agents;
		NativeArray<int> childPointers;
		NativeArray<float3> boundingBoxBuffer;
		NativeArray<int> agentCountBuffer;
		NativeArray<float3> agentPositions;
		NativeArray<float> agentRadii;
		NativeArray<float> maxSpeeds;
		NativeArray<float> maxRadius;
		NativeArray<float> nodeAreas;
		MovementPlane movementPlane;

		const int LeafNodeBit = 1 << 30;
		const int BitPackingShift = 15;
		const int BitPackingMask = (1 << BitPackingShift) - 1;
		const int MaxAgents = BitPackingMask;

		/// <summary>
		/// For a given number, contains the index of the first non-zero bit.
		/// Only the values 0 through 15 are used when movementPlane is XZ or XY.
		///
		/// Use bytes instead of ints to save some precious L1 cache memory.
		/// </summary>
		static readonly byte[] ChildLookup = new byte[256];

		static RVOQuadtreeBurst()
        {
        }

        public Rect bounds
        {
            get
            {
                return boundingBoxBuffer.IsCreated ? Rect.MinMaxRect(boundingBoxBuffer[0].x, boundingBoxBuffer[0].y, boundingBoxBuffer[1].x, boundingBoxBuffer[1].y) : new Rect();
            }
        }

        static int InnerNodeCountUpperBound(int numAgents, MovementPlane movementPlane)
        {
            return default;
        }

        public void Dispose()
        {
        }

        void Reserve(int minSize)
        {
        }

        public JobBuild BuildJob(NativeArray<float3> agentPositions, NativeArray<AgentIndex> agentVersions, NativeArray<float> agentSpeeds, NativeArray<float> agentRadii, int numAgents, MovementPlane movementPlane)
        {
            return default;
        }

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
        public struct JobBuild : IJob
        {
            /// <summary>Length should be greater or equal to agentPositions.Length</summary>
            public NativeArray<int> agents;

			[ReadOnly]
			public NativeArray<float3> agentPositions;

			[ReadOnly]
			public NativeArray<AgentIndex> agentVersions;

			[ReadOnly]
			public NativeArray<float> agentSpeeds;

			[ReadOnly]
			public NativeArray<float> agentRadii;

			/// <summary>Should have size 2</summary>
			[WriteOnly]
			public NativeArray<float3> outBoundingBox;

			/// <summary>Should have size 1</summary>
			[WriteOnly]
			public NativeArray<int> outAgentCount;

			/// <summary>Should have size: InnerNodeCountUpperBound(numAgents)</summary>
			public NativeArray<int> outChildPointers;

			/// <summary>Should have size: InnerNodeCountUpperBound(numAgents)</summary>
			public NativeArray<float> outMaxSpeeds;

			/// <summary>Should have size: InnerNodeCountUpperBound(numAgents)</summary>
			public NativeArray<float> outMaxRadius;

			/// <summary>Should have size: InnerNodeCountUpperBound(numAgents)</summary>
			public NativeArray<float> outArea;

			[WriteOnly]
			public NativeArray<float3> outAgentPositions;

			[WriteOnly]
			public NativeArray<float> outAgentRadii;

			public int numAgents;

			public MovementPlane movementPlane;

			static int Partition (NativeSlice<int> indices, int startIndex, int endIndex, NativeSlice<float> coordinates, float splitPoint) {
                return default;
            }

            void BuildNode(float3 boundsMin, float3 boundsMax, int depth, int agentsStart, int agentsEnd, int nodeOffset, ref int firstFreeChild)
            {
            }

            void CalculateSpeeds(int nodeCount)
            {
            }

            public void Execute()
            {
            }
        }

		public struct QuadtreeQuery {
			public float3 position;
			public float speed, timeHorizon, agentRadius;
			public int outputStartIndex, maxCount;
			public RVOLayer layerMask;
			public NativeArray<RVOLayer> layers;
			public NativeArray<int> result;
			public NativeArray<float> resultDistances;
		}

		/// <summary>
		/// A very large distance. Used as a sentinel value in the QueryKNearest method.
		/// We don't use actual infinity, because the code may be compiled using FastMath, which makes the compiler assume that infinities do not exist.
		/// This should be much larger than any distance used in practice.
		/// </summary>
		const float DistanceInfinity = 1e30f;

		public int QueryKNearest (QuadtreeQuery query)
        {
            return default;
        }

        void QueryRec(ref QuadtreeQuery query, int treeNodeIndex, float3 nodeMin, float3 nodeMax, ref float maxRadius)
        {
        }

        /// <summary>Find the total agent area inside the circle at position with the given radius</summary>
        public float QueryArea(float3 position, float radius)
        {
            return default;
        }

        float QueryAreaRec(int treeNodeIndex, float3 p, float radius, float3 nodeMin, float3 nodeMax)
        {
            return default;
        }

        [BurstCompile]
        public struct DebugDrawJob : IJob
        {
            public CommandBuilder draw;
            [ReadOnly]
            public RVOQuadtreeBurst quadtree;

            public void Execute()
            {
            }
        }

        public void DebugDraw(CommandBuilder draw)
        {
        }

        void DebugDraw(int nodeIndex, float3 nodeMin, float3 nodeMax, CommandBuilder draw)
        {
        }
    }
}
