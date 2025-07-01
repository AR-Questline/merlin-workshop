using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding.RVO {
	using Pathfinding;
	using Pathfinding.Util;
	using Unity.Burst;
	using Unity.Jobs;
	using Unity.Mathematics;
	using Unity.Collections;
	using Pathfinding.Collections;
	using Pathfinding.Drawing;
	using Pathfinding.ECS.RVO;
	using static Unity.Burst.CompilerServices.Aliasing;
	using Unity.Profiling;
	using System.Diagnostics;

	[BurstCompile(CompileSynchronously = false, FloatMode = FloatMode.Fast)]
	public struct JobRVOPreprocess : IJob {
		[ReadOnly]
		public SimulatorBurst.AgentData agentData;

		[ReadOnly]
		public SimulatorBurst.AgentOutputData previousOutput;

		[WriteOnly]
		public SimulatorBurst.TemporaryAgentData temporaryAgentData;

		public int startIndex;
		public int endIndex;

		public void Execute () {
        }
    }

	/// <summary>
	/// Inspired by StarCraft 2's avoidance of locked units.
	/// See: http://www.gdcvault.com/play/1014514/AI-Navigation-It-s-Not
	/// </summary>
	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobHorizonAvoidancePhase1 : Pathfinding.Jobs.IJobParallelForBatched {
		[ReadOnly]
		public SimulatorBurst.AgentData agentData;

		[ReadOnly]
		public NativeArray<float2> desiredTargetPointInVelocitySpace;

		[ReadOnly]
		public NativeArray<int> neighbours;

		public SimulatorBurst.HorizonAgentData horizonAgentData;

		public CommandBuilder draw;

		public bool allowBoundsChecks { get { return true; } }

		/// <summary>
		/// Super simple bubble sort.
		/// TODO: This will be replaced by a better implementation from the Unity.Collections library when that is stable.
		/// </summary>
		static void Sort<T>(NativeSlice<T> arr, NativeSlice<float> keys) where T : struct {
        }


        /// <summary>Calculates the shortest difference between two given angles given in radians.</summary>
        public static float DeltaAngle(float current, float target)
        {
            return default;
        }

        public void Execute(int startIndex, int count)
        {
        }
    }

	/// <summary>
	/// Inspired by StarCraft 2's avoidance of locked units.
	/// See: http://www.gdcvault.com/play/1014514/AI-Navigation-It-s-Not
	/// </summary>
	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobHorizonAvoidancePhase2 : Pathfinding.Jobs.IJobParallelForBatched {
		[ReadOnly]
		public NativeArray<int> neighbours;
		[ReadOnly]
		public NativeArray<AgentIndex> versions;
		public NativeArray<float3> desiredVelocity;
		public NativeArray<float2> desiredTargetPointInVelocitySpace;

		[ReadOnly]
		public NativeArray<NativeMovementPlane> movementPlane;

		public SimulatorBurst.HorizonAgentData horizonAgentData;

		public bool allowBoundsChecks => false;

		public void Execute (int startIndex, int count) {
        }
    }

	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobHardCollisions<MovementPlaneWrapper> : Pathfinding.Jobs.IJobParallelForBatched where MovementPlaneWrapper : struct, IMovementPlaneWrapper {
		[ReadOnly]
		public SimulatorBurst.AgentData agentData;
		[ReadOnly]
		public NativeArray<int> neighbours;
		[WriteOnly]
		public NativeArray<float2> collisionVelocityOffsets;

		public float deltaTime;
		public bool enabled;

		/// <summary>
		/// How aggressively hard collisions are resolved.
		/// Should be a value between 0 and 1.
		/// </summary>
		const float CollisionStrength = 0.8f;

		public bool allowBoundsChecks => false;

		public void Execute (int startIndex, int count) {
        }
    }

	[BurstCompile(CompileSynchronously = false, FloatMode = FloatMode.Fast)]
	public struct JobRVOCalculateNeighbours<MovementPlaneWrapper> : Pathfinding.Jobs.IJobParallelForBatched where MovementPlaneWrapper : struct, IMovementPlaneWrapper {
		[ReadOnly]
		public SimulatorBurst.AgentData agentData;

		[ReadOnly]
		public RVOQuadtreeBurst quadtree;

		public NativeArray<int> outNeighbours;

		[WriteOnly]
		public SimulatorBurst.AgentOutputData output;

		public bool allowBoundsChecks { get { return false; } }

		public void Execute (int startIndex, int count) {
        }

        void CalculateNeighbours(int agentIndex, NativeArray<int> neighbours, NativeArray<float> neighbourDistances)
        {
        }
    }

	/// <summary>
	/// Calculates if the agent has reached the end of its path and if its blocked from further progress towards it.
	///
	/// If many agents have the same destination they can often end up crowded around a single point.
	/// It is often desirable to detect this and mark all agents around that destination as having at least
	/// partially reached the end of their paths.
	///
	/// This job uses the following heuristics to determine this:
	///
	/// 1. If an agent wants to move in a particular direction, but there's another agent in the way that makes it have to reduce its velocity,
	///     the other agent is considered to be "blocking" the current agent.
	/// 2. If the agent is within a small distance of the destination
	///        THEN it is considered to have reached the end of its path.
	/// 3. If the agent is blocked by another agent,
	///        AND the other agent is blocked by this agent in turn,
	///        AND if the destination is between the two agents,
	///        THEN the the agent is considered to have reached the end of its path.
	/// 4. If the agent is blocked by another agent which has reached the end of its path,
	///        AND this agent is is moving slowly
	///        AND this agent cannot move furter forward than 50% of its radius.
	///        THEN the agent is considered to have reached the end of its path.
	///
	/// Heuristics 2 and 3 are calculated initially, and then using heuristic 4 the set of agents which have reached their destinations expands outwards.
	///
	/// These heuristics are robust enough that they can be used even if for example the agents are stuck in a winding maze
	/// and only one agent is actually able to reach the destination.
	///
	/// This job doesn't affect the movement of the agents by itself.
	/// However, it is built with the intention that the FlowFollowingStrength parameter will be set
	/// elsewhere to 1 for agents which have reached the end of their paths. This will make the agents stop gracefully
	/// when the end of their paths is crowded instead of continuing to try to desperately reach the destination.
	/// </summary>
	[BurstCompile(CompileSynchronously = false, FloatMode = FloatMode.Fast)]
	public struct JobDestinationReached<MovementPlaneWrapper>: IJob where MovementPlaneWrapper : struct, IMovementPlaneWrapper {
		[ReadOnly]
		public SimulatorBurst.AgentData agentData;

		[ReadOnly]
		public SimulatorBurst.TemporaryAgentData temporaryAgentData;

		[ReadOnly]
		public SimulatorBurst.ObstacleData obstacleData;

		public SimulatorBurst.AgentOutputData output;
		public int numAgents;
		public CommandBuilder draw;

		private static readonly ProfilerMarker MarkerInvert = new ProfilerMarker("InvertArrows");
		private static readonly ProfilerMarker MarkerAlloc = new ProfilerMarker("Alloc");
		private static readonly ProfilerMarker MarkerFirstPass = new ProfilerMarker("FirstPass");

		struct TempAgentData {
			public bool blockedAndSlow;
			public float distToEndSq;
		}

		public void Execute ()
        {
        }
    }

    // Note: FloatMode should not be set to Fast because that causes inaccuracies which can lead to
    // agents failing to avoid walls sometimes.
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Default)]
    public struct JobRVO<MovementPlaneWrapper> : Pathfinding.Jobs.IJobParallelForBatched where MovementPlaneWrapper : struct, IMovementPlaneWrapper
    {
        [ReadOnly]
        public SimulatorBurst.AgentData agentData;

        [ReadOnly]
        public SimulatorBurst.TemporaryAgentData temporaryAgentData;

        [ReadOnly]
        public NavmeshEdges.NavmeshBorderData navmeshEdgeData;

        [WriteOnly]
        public SimulatorBurst.AgentOutputData output;

        public float deltaTime;
        public float symmetryBreakingBias;
        public float priorityMultiplier;
        public bool useNavmeshAsObstacle;

        public bool allowBoundsChecks { get { return true; } }

        const int MaxObstacleCount = 50;

        public CommandBuilder draw;

        public void Execute(int startIndex, int batchSize)
        {
        }

        struct SortByKey : IComparer<int>
        {
            public UnsafeSpan<float> keys;

            public int Compare(int x, int y)
            {
                return default;
            }
        }

        /// <summary>
        /// Sorts the array in place using insertion sort.
        /// This is a stable sort.
        /// See: http://en.wikipedia.org/wiki/Insertion_sort
        ///
        /// Used only because Unity.Collections.NativeSortExtension.Sort seems to have some kind of code generation bug when using Burst 1.8.2, causing it to throw exceptions.
        /// </summary>
        static void InsertionSort<T, U>(UnsafeSpan<T> data, U comparer) where T : unmanaged where U : IComparer<T>
        {
        }

        private static readonly ProfilerMarker MarkerConvertObstacles1 = new ProfilerMarker("RVOConvertObstacles1");
        private static readonly ProfilerMarker MarkerConvertObstacles2 = new ProfilerMarker("RVOConvertObstacles2");

        /// <summary>
        /// Generates ORCA half-planes for all obstacles near the agent.
        /// For more details refer to the ORCA (Optimal Reciprocal Collision Avoidance) paper.
        ///
        /// This function takes in several arrays which are just used for temporary data. This is to avoid the overhead of allocating the arrays once for every agent.
        /// </summary>
        void GenerateObstacleVOs(int agentIndex, NativeList<int> adjacentObstacleIdsScratch, NativeArray<int2> adjacentObstacleVerticesScratch, NativeArray<float> segmentDistancesScratch, NativeArray<int> sortedVerticesScratch, NativeArray<ORCALine> orcaLines, NativeArray<int> orcaLineToAgent, [NoAlias] ref int numLines, [NoAlias] in MovementPlaneWrapper movementPlane, float2 optimalVelocity)
        {
        }

        public void ExecuteORCA(int startIndex, int batchSize)
        {
        }

        /// <summary>
        /// Find the distance we can move towards our target without colliding with anything.
        /// May become negative if we are currently colliding with something.
        /// </summary>
        float CalculateForwardClearance(NativeSlice<int> neighbours, MovementPlaneWrapper movementPlane, float3 position, float radius, float2 targetDir)
        {
            return default;
        }

        /// <summary>True if vector2 is to the left of vector1 or if they are colinear.</summary>
        static bool leftOrColinear(float2 vector1, float2 vector2)
        {
            return default;
        }

        /// <summary>True if vector2 is to the left of vector1.</summary>
        static bool left(float2 vector1, float2 vector2)
        {
            return default;
        }

        /// <summary>True if vector2 is to the right of vector1 or if they are colinear.</summary>
        static bool rightOrColinear(float2 vector1, float2 vector2)
        {
            return default;
        }

        /// <summary>True if vector2 is to the right of vector1.</summary>
        static bool right(float2 vector1, float2 vector2)
        {
            return default;
        }

        /// <summary>
        /// Determinant of the 2x2 matrix defined by vector1 and vector2.
        /// Alternatively, the Z component of the cross product of vector1 and vector2.
        /// </summary>
        static float det(float2 vector1, float2 vector2)
        {
            return default;
        }

        static float2 rot90(float2 v)
        {
            return default;
        }

        /// <summary>
        /// A half-plane defined as the line splitting plane.
        ///
        /// For ORCA purposes, the infeasible region of the half-plane is on the right side of the line.
        /// </summary>
        struct ORCALine
        {
            public float2 point;
            public float2 direction;

            public void DrawAsHalfPlane(CommandBuilder draw, float halfPlaneLength, float halfPlaneWidth, Color color)
            {
            }

            public ORCALine(float2 position, float2 relativePosition, float2 velocity, float2 otherVelocity, float combinedRadius, float timeStep, float invTimeHorizon) : this()
            {
            }
        }

        /// <summary>
        /// Calculates how far inside the infeasible region of the ORCA half-planes the velocity is.
        /// Returns 0 if the velocity is in the feasible region of all half-planes.
        /// </summary>
        static float DistanceInsideVOs(UnsafeSpan<ORCALine> lines, float2 velocity)
        {
            return default;
        }

        /// <summary>
        /// Bias towards the right side of agents.
        /// Rotate desiredVelocity at most [value] number of radians. 1 radian ≈ 57°
        /// This breaks up symmetries.
        ///
        /// The desired velocity will only be rotated if it is inside a velocity obstacle (VO).
        /// If it is inside one, it will not be rotated further than to the edge of it
        ///
        /// The targetPointInVelocitySpace will be rotated by the same amount as the desired velocity
        ///
        /// Returns: True if the desired velocity was inside any VO
        /// </summary>
        static bool BiasDesiredVelocity(UnsafeSpan<ORCALine> lines, ref float2 desiredVelocity, ref float2 targetPointInVelocitySpace, float maxBiasRadians)
        {
            return default;
        }

        /// <summary>
        /// Clip a line to the feasible region of the half-plane given by the clipper.
        /// The clipped line is `line.point + line.direction*tLeft` to `line.point + line.direction*tRight`.
        ///
        /// Returns false if the line is parallel to the clipper's border.
        /// </summary>
        static bool ClipLine(ORCALine line, ORCALine clipper, ref float tLeft, ref float tRight)
        {
            return default;
        }

        static bool ClipBoundary(NativeArray<ORCALine> lines, int lineIndex, float radius, out float tLeft, out float tRight)
        {
            tLeft = default(float);
            tRight = default(float);
            return default;
        }

        static bool LinearProgram1D(NativeArray<ORCALine> lines, int lineIndex, float radius, float2 optimalVelocity, bool directionOpt, ref float2 result)
        {
            return default;
        }

        struct LinearProgram2Output {
			public float2 velocity;
            public int firstFailedLineIndex;
        }

        static LinearProgram2Output LinearProgram2D(NativeArray<ORCALine> lines, int numLines, float radius, float2 optimalVelocity, bool directionOpt)
        {
            return default;
        }

        static float ClosestPointOnSegment (float2 a, float2 dir, float2 p, float t0, float t1) {
            return default;
        }

        /// <summary>
        /// Closest point on segment a to segment b.
        /// The segments are given by infinite lines and bounded by t values. p = line.point + line.dir*t.
        ///
        /// It is assumed that the two segments do not intersect.
        /// </summary>
        static float2 ClosestSegmentSegmentPointNonIntersecting(ORCALine a, ORCALine b, float ta1, float ta2, float tb1, float tb2)
        {
            return default;
        }

        /// <summary>Like LinearProgram2D, but the optimal velocity space is a segment instead of a point, however the current result has collapsed to a point</summary>
        static LinearProgram2Output LinearProgram2DCollapsedSegment(NativeArray<ORCALine> lines, int numLines, int startLine, float radius, float2 currentResult, float2 optimalVelocityStart, float2 optimalVelocityDir, float optimalTLeft, float optimalTRight)
        {
            return default;
        }

        /// <summary>Like LinearProgram2D, but the optimal velocity space is a segment instead of a point</summary>
        static LinearProgram2Output LinearProgram2DSegment(NativeArray<ORCALine> lines, int numLines, float radius, float2 optimalVelocityStart, float2 optimalVelocityDir, float optimalTLeft, float optimalTRight, float optimalT)
        {
            return default;
        }

        /// <summary>
        /// Finds the velocity with the smallest maximum penetration into the given half-planes.
        ///
        /// Assumes there are no points in the feasible region of the given half-planes.
        ///
        /// Runs a 3-dimensional linear program, but projected down to 2D.
        /// If there are no feasible regions outside all half-planes then we want to find the velocity
        /// for which the maximum penetration into infeasible regions is minimized.
        /// Conceptually we can solve this by taking our half-planes, and moving them outwards at a fixed speed
        /// until there is exactly 1 feasible point.
        /// We can formulate this in 3D space by thinking of the half-planes in 3D (velocity.x, velocity.y, penetration-depth) space, as sloped planes.
        /// Moving the planes outwards then corresponds to decreasing the z coordinate.
        /// In 3D space we want to find the point above all planes with the lowest z coordinate.
        /// We do this by going through each plane and testing if it is possible that this plane
        /// is the one with the maximum penetration.
        /// If so, we know that the point will lie on the portion of that plane bounded by the intersections
        /// with the other planes. We generate projected half-planes which represent the intersections with the
        /// other 3D planes, and then we run a new optimization to find the point which penetrates this
        /// half-plane the least.
        /// </summary>
        /// <param name="lines">The half-planes of all obstacles and agents.</param>
        /// <param name="numLines">The number of half-planes in lines.</param>
        /// <param name="numFixedLines">The number of half-planes in lines which are fixed (0..numFixedLines). These will be treated as static obstacles which should be avoided at all costs.</param>
        /// <param name="beginLine">The index of the first half-plane in lines for which the previous optimization failed (see \reflink{LinearProgram2Output.firstFailedLineIndex}).</param>
        /// <param name="radius">Maximum possible speed. This represents a circular velocity obstacle.</param>
        /// <param name="result">Input is best velocity as output by \reflink{LinearProgram2D}. Output is the new best velocity. The velocity with the smallest maximum penetration into the given half-planes.</param>
        /// <param name="scratchBuffer">A buffer of length at least numLines to use for scratch space.</param>
        static void LinearProgram3D(NativeArray<ORCALine> lines, int numLines, int numFixedLines, int beginLine, float radius, ref float2 result, NativeArray<ORCALine> scratchBuffer)
        {
        }

        static void DrawVO(CommandBuilder draw, float2 circleCenter, float radius, float2 origin, Color color)
        {
        }
    }
}
