using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Pooling;
using Unity.Mathematics;

namespace Pathfinding {
	/// <summary>
	/// A path which searches from one point to a number of different targets in one search or from a number of different start points to a single target.
	///
	/// This is faster than searching with an ABPath for each target if pathsForAll is true.
	/// This path type can be used for example when you want an agent to find the closest target of a few different options.
	///
	/// When pathsForAll is true, it will calculate a path to each target point, but it can share a lot of calculations for the different paths so
	/// it is faster than requesting them separately.
	///
	/// When pathsForAll is false, it will perform a search using the heuristic set to None and stop as soon as it finds the first target.
	/// This may be faster or slower than requesting each path separately.
	/// It will run a Dijkstra search where it searches all nodes around the start point until the closest target is found.
	/// Note that this is usually faster if some target points are very close to the start point and some are very far away, but
	/// it can be slower if all target points are relatively far away because then it will have to search a much larger
	/// region since it will not use any heuristics.
	///
	/// See: Seeker.StartMultiTargetPath
	/// See: MultiTargetPathExample.cs (view in online documentation for working links) "Example of how to use multi-target-paths"
	///
	/// Version: Since 3.7.1 the vectorPath and path fields are always set to the shortest path even when pathsForAll is true.
	/// </summary>
	public class MultiTargetPath : ABPath {
		/// <summary>Callbacks to call for each individual path</summary>
		public OnPathDelegate[] callbacks;

		/// <summary>Nearest nodes to the <see cref="targetPoints"/></summary>
		public GraphNode[] targetNodes;

		/// <summary>Number of target nodes left to find</summary>
		protected int targetNodeCount;

		/// <summary>Indicates if the target has been found. Also true if the target cannot be reached (is in another area)</summary>
		public bool[] targetsFound;

		/// <summary>The cost of the calculated path for each target. Will be 0 if a path was not found.</summary>
		public uint[] targetPathCosts;

		/// <summary>Target points specified when creating the path. These are snapped to the nearest nodes</summary>
		public Vector3[] targetPoints;

		/// <summary>Target points specified when creating the path. These are not snapped to the nearest nodes</summary>
		public Vector3[] originalTargetPoints;

		/// <summary>Stores all vector paths to the targets. Elements are null if no path was found</summary>
		public List<Vector3>[] vectorPaths;

		/// <summary>Stores all paths to the targets. Elements are null if no path was found</summary>
		public List<GraphNode>[] nodePaths;

		/// <summary>If true, a path to all targets will be returned, otherwise just the one to the closest one.</summary>
		public bool pathsForAll = true;

		/// <summary>The closest target index (if any target was found)</summary>
		public int chosenTarget = -1;

		/// <summary>False if the path goes from one point to multiple targets. True if it goes from multiple start points to one target point</summary>
		public bool inverted { get; protected set; }

		public override bool endPointKnownBeforeCalculation => false;

		/// <summary>
		/// Default constructor.
		/// Do not use this. Instead use the static Construct method which can handle path pooling.
		/// </summary>
		public MultiTargetPath () {}

        public static MultiTargetPath Construct (Vector3[] startPoints, Vector3 target, OnPathDelegate[] callbackDelegates, OnPathDelegate callback = null) {
            return default;
        }

        public static MultiTargetPath Construct (Vector3 start, Vector3[] targets, OnPathDelegate[] callbackDelegates, OnPathDelegate callback = null) {
            return default;
        }

        protected void Setup (Vector3 start, Vector3[] targets, OnPathDelegate[] callbackDelegates, OnPathDelegate callback) {
        }

        protected override void Reset()
        {
        }

        protected override void OnEnterPool()
        {
        }

        /// <summary>Set chosenTarget to the index of the shortest path</summary>
        void ChooseShortestPath()
        {
        }

        void SetPathParametersForReturn(int target)
        {
        }

        protected override void ReturnPath()
        {
        }

        protected void RebuildOpenList()
        {
        }

        protected override void Prepare()
        {
        }

        void RecalculateHTarget()
        {
        }

        protected override void Cleanup()
        {
        }

        protected override void OnHeapExhausted () {
        }

        protected override void OnFoundEndNode(uint pathNode, uint hScore, uint gScore)
        {
        }

        protected override void Trace (uint pathNodeIndex) {
        }

        protected override string DebugString(PathLog logMode)
        {
            return default;
        }
    }
}
