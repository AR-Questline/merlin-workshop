//#define ASTAR_POOL_DEBUG // Enables debugging of path pooling. Will log warnings and info messages about paths not beeing pooled correctly.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Pathfinding.Collections;
using Pathfinding.Pooling;

namespace Pathfinding {
	/// <summary>Base class for all path types</summary>
	[Unity.Burst.BurstCompile]
	public abstract class Path : IPathInternals {
#if ASTAR_POOL_DEBUG
		private string pathTraceInfo = "";
		private List<string> claimInfo = new List<string>();
		~Path() {
			Debug.Log("Destroying " + GetType().Name + " instance");
			if (claimed.Count > 0) {
				Debug.LogWarning("Pool Is Leaking. See list of claims:\n" +
					"Each message below will list what objects are currently claiming the path." +
					" These objects have removed their reference to the path object but has not called .Release on it (which is bad).\n" + pathTraceInfo+"\n");
				for (int i = 0; i < claimed.Count; i++) {
					Debug.LogWarning("- Claim "+ (i+1) + " is by a " + claimed[i].GetType().Name + "\n"+claimInfo[i]);
				}
			} else {
				Debug.Log("Some scripts are not using pooling.\n" + pathTraceInfo + "\n");
			}
		}
#endif

		/// <summary>Data for the thread calculating this path</summary>
		protected PathHandler pathHandler;

		/// <summary>
		/// Callback to call when the path is complete.
		/// This is usually sent to the Seeker component which post processes the path and then calls a callback to the script which requested the path
		/// </summary>
		public OnPathDelegate callback;

		/// <summary>
		/// Immediate callback to call when the path is complete.
		/// Warning: This may be called from a separate thread. Usually you do not want to use this one.
		///
		/// See: callback
		/// </summary>
		public OnPathDelegate immediateCallback;

		/// <summary>Returns the state of the path in the pathfinding pipeline</summary>
		public PathState PipelineState { get; private set; }

		/// <summary>
		/// Provides additional traversal information to a path request.
		/// See: traversal_provider (view in online documentation for working links)
		/// </summary>
		public ITraversalProvider traversalProvider;


		/// <summary>Backing field for <see cref="CompleteState"/></summary>
		protected PathCompleteState completeState;

		/// <summary>
		/// Current state of the path.
		/// Bug: This may currently be set to Complete before the path has actually been fully calculated. In particular the vectorPath and path lists may not have been fully constructed.
		/// This can lead to race conditions when using multithreading. Try to avoid using this method to check for if the path is calculated right now, use <see cref="IsDone"/> instead.
		/// </summary>
		public PathCompleteState CompleteState {
			get { return completeState; }
			protected set {
				// Locking is used to avoid multithreading race conditions in which, for example,
				// the error state is set on the main thread to cancel the path,
				// and then a pathfinding thread marks the path as completed,
				// which would replace the error state (if a lock and check would not have been used).
				// We lock on the path object itself. Users should rarely have to use the path object
				// themselves for anything before the path is calculated, much less take a lock on it.
				lock (this) {
					// Once the path is put in the error state, it cannot be set to any other state
					if (completeState != PathCompleteState.Error) completeState = value;
				}
			}
		}

		/// <summary>
		/// If the path failed, this is true.
		///
		/// This typically happens if there's no valid node close enough to the start point of the path,
		/// or if there's no node close enough to the target point that is reachable from the start point.
		/// The <see cref="errorLog"/> will have more information about what happened.
		///
		/// See: <see cref="errorLog"/>
		/// See: error-messages (view in online documentation for working links)
		/// See: This is equivalent to checking path.CompleteState == PathCompleteState.Error
		/// </summary>
		public bool error { get { return CompleteState == PathCompleteState.Error; } }

		/// <summary>
		/// Additional info on why a path failed.
		/// See: <see cref="AstarPath.logPathResults"/>
		/// See: error-messages (view in online documentation for working links)
		/// </summary>
		public string errorLog { get; private set; }

		/// <summary>
		/// Holds the path as a <see cref="GraphNode"/> list.
		///
		/// These are all nodes that the path traversed, as calculated by the pathfinding algorithm.
		/// This may not be the same nodes as the post processed path traverses.
		///
		/// See: <see cref="vectorPath"/>
		/// </summary>
		public List<GraphNode> path;

		/// <summary>
		/// Holds the (possibly post-processed) path as a Vector3 list.
		///
		/// This list may be modified by path modifiers to be smoother or simpler compared to the raw path generated by the pathfinding algorithm.
		///
		/// See: modifiers (view in online documentation for working links)
		/// See: <see cref="path"/>
		/// </summary>
		public List<Vector3> vectorPath;

		/// <summary>How long it took to calculate this path in milliseconds</summary>
		public float duration;

		/// <summary>Number of nodes this path has searched</summary>
		public int searchedNodes { get; protected set; }

		/// <summary>
		/// True if the path is currently pooled.
		/// Do not set this value. Only read. It is used internally.
		///
		/// See: <see cref="PathPool"/>
		/// </summary>
		bool IPathInternals.Pooled { get; set; }

		/// <summary>
		/// True if the Reset function has been called.
		/// Used to alert users when they are doing something wrong.
		/// </summary>
		protected bool hasBeenReset;

		/// <summary>Constraint for how to search for nodes</summary>
		public NNConstraint nnConstraint = PathNNConstraint.Walkable;

		/// <summary>Determines which heuristic to use</summary>
		public Heuristic heuristic;

		/// <summary>
		/// Scale of the heuristic values.
		/// See: AstarPath.heuristicScale
		/// </summary>
		public float heuristicScale = 1F;

		/// <summary>ID of this path. Used to distinguish between different paths</summary>
		public ushort pathID { get; private set; }

		/// <summary>Target to use for H score calculation.</summary>
		protected GraphNode hTargetNode;

		/// <summary>
		/// Target to use for H score calculations.
		/// See: https://en.wikipedia.org/wiki/Admissible_heuristic
		/// </summary>
		protected HeuristicObjective heuristicObjective;

		internal ref HeuristicObjective heuristicObjectiveInternal => ref heuristicObjective;

		/// <summary>
		/// Which graph tags are traversable.
		/// This is a bitmask so -1 = all bits set = all tags traversable.
		/// For example, to set bit 5 to true, you would do
		/// <code> myPath.enabledTags |= 1 << 5; </code>
		/// To set it to false, you would do
		/// <code> myPath.enabledTags &= ~(1 << 5); </code>
		///
		/// The Seeker has a popup field where you can set which tags to use.
		/// Note: If you are using a Seeker. The Seeker will set this value to what is set in the inspector field on StartPath.
		/// So you need to change the Seeker value via script, not set this value if you want to change it via script.
		///
		/// See: <see cref="CanTraverse"/>
		/// See: bitmasks (view in online documentation for working links)
		/// </summary>
		public int enabledTags = -1;

		/// <summary>List of zeroes to use as default tag penalties</summary>
		internal static readonly int[] ZeroTagPenalties = new int[32];

		/// <summary>
		/// The tag penalties that are actually used.
		/// See: <see cref="tagPenalties"/>
		/// </summary>
		protected int[] internalTagPenalties;

		/// <summary>
		/// Penalties for each tag.
		/// Tag 0 which is the default tag, will get a penalty of tagPenalties[0].
		/// These should only be non-negative values since the A* algorithm cannot handle negative penalties.
		///
		/// When assigning an array to this property it must have a length of 32.
		///
		/// Note: Setting this to null will make all tag penalties be treated as if they are zero.
		///
		/// Note: If you are using a Seeker. The Seeker will set this value to what is set in the inspector field when you call seeker.StartPath.
		/// So you need to change the Seeker's value via script, not set this value.
		///
		/// See: <see cref="Seeker.tagPenalties"/>
		/// </summary>
		public int[] tagPenalties {
			get {
				return internalTagPenalties == ZeroTagPenalties ? null : internalTagPenalties;
			}
			set {
				if (value == null) {
					internalTagPenalties = ZeroTagPenalties;
				} else {
					if (value.Length != 32) throw new System.ArgumentException("tagPenalties must have a length of 32");

					internalTagPenalties = value;
				}
			}
		}

		/// <summary>Copies the given settings into this path</summary>
		public void UseSettings (PathRequestSettings settings) {
        }

        /// <summary>
        /// Total Length of the path.
        /// Calculates the total length of the <see cref="vectorPath"/>.
        /// Cache this rather than call this function every time since it will calculate the length every time, not just return a cached value.
        /// Returns: Total length of <see cref="vectorPath"/>, if <see cref="vectorPath"/> is null positive infinity is returned.
        /// </summary>
        public float GetTotalLength () {
            return default;
        }

        /// <summary>
        /// Waits until this path has been calculated and returned.
        /// Allows for very easy scripting.
        ///
        /// <code>
        /// IEnumerator Start () {
        ///     // Get the seeker component attached to this GameObject
        ///     var seeker = GetComponent<Seeker>();
        ///
        ///     var path = seeker.StartPath(transform.position, transform.position + Vector3.forward * 10, null);
        ///     // Wait... This may take a frame or two depending on how complex the path is
        ///     // The rest of the game will continue to run while we wait
        ///     yield return StartCoroutine(path.WaitForPath());
        ///     // The path is calculated now
        ///
        ///     // Draw the path in the scene view for 10 seconds
        ///     for (int i = 0; i < path.vectorPath.Count - 1; i++) {
        ///         Debug.DrawLine(path.vectorPath[i], path.vectorPath[i+1], Color.red, 10);
        ///     }
        /// }
        /// </code>
        ///
        /// Note: Do not confuse this with AstarPath.BlockUntilCalculated. This one will wait using yield until it has been calculated
        /// while AstarPath.BlockUntilCalculated will halt all operations until the path has been calculated.
        ///
        /// Throws: System.InvalidOperationException if the path is not started. Send the path to <see cref="Seeker.StartPath(Path)"/> or <see cref="AstarPath.StartPath"/> before calling this function.
        ///
        /// See: <see cref="BlockUntilCalculated"/>
        /// See: https://docs.unity3d.com/Manual/Coroutines.html
        /// </summary>
        public IEnumerator WaitForPath()
        {
            return default;
        }

        /// <summary>
        /// Blocks until this path has been calculated and returned.
        /// Normally it takes a few frames for a path to be calculated and returned.
        /// This function will ensure that the path will be calculated when this function returns
        /// and that the callback for that path has been called.
        ///
        /// Use this function only if you really need to.
        /// There is a point to spreading path calculations out over several frames.
        /// It smoothes out the framerate and makes sure requesting a large
        /// number of paths at the same time does not cause lag.
        ///
        /// Note: Graph updates and other callbacks might get called during the execution of this function.
        ///
        /// <code>
        /// var path = seeker.StartPath(transform.position, transform.position + Vector3.forward * 10, OnPathComplete);
        /// path.BlockUntilCalculated();
        ///
        /// // The path is calculated now, and the OnPathComplete callback has been called
        /// </code>
        ///
        /// See: This is equivalent to calling <see cref="AstarPath.BlockUntilCalculated(Path)"/>
        /// See: <see cref="WaitForPath"/>
        /// </summary>
        public void BlockUntilCalculated()
        {
        }

        /// <summary>
        /// True if this path node might be worth exploring.
        ///
        /// This is used during a search to filter out nodes which have already been fully searched.
        /// </summary>
        public bool ShouldConsiderPathNode(uint pathNodeIndex)
        {
            return default;
        }

        public static readonly Unity.Profiling.ProfilerMarker MarkerOpenCandidateConnectionsToEnd = new Unity.Profiling.ProfilerMarker("OpenCandidateConnectionsToEnd");
        public static readonly Unity.Profiling.ProfilerMarker MarkerTrace = new Unity.Profiling.ProfilerMarker("Trace");

        /// <summary>
        /// Paths use this to skip adding nodes to the search heap.
        ///
        /// This is used by triangle nodes if they find an edge which is identical (but reversed) to an edge in an adjacent node.
        /// This means that it cannot be better to visit the adjacent node's edge from any other way than what we are currently considering.
        /// Therefore, instead of adding the node to the heap, only to pop it in the next iteration, we can skip that step and save some processing time.
        ///
        /// After calling this function, the skipped node should be immediately opened, so that it can be searched.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipOverNode(uint pathNodeIndex, uint parentNodeIndex, uint fractionAlongEdge, uint hScore, uint gScore)
        {
        }

        /// <summary>
        /// Open a connection to the temporary end node if necessary.
        ///
        /// The start and end nodes are temporary nodes and are not included in the graph itself.
        /// This means that we need to handle connections to and from those nodes as a special case.
        /// This function will open a connection from the given node to the end node, if such a connection exists.
        ///
        /// It is called from the <see cref="GraphNode.Open"/> function.
        /// </summary>
        /// <param name="position">Position of the path node that is being opened. This may be different from the node's position if \reflink{PathNode.fractionAlongEdge} is being used.</param>
        /// <param name="parentPathNode">Index of the path node that is being opened. This is often the same as parentNodeIndex, but may be different if the node has multiple path node variants.</param>
        /// <param name="parentNodeIndex">Index of the node that is being opened.</param>
        /// <param name="parentG">G score of the parent node. The cost to reach the parent node from the start of the path.</param>
        public void OpenCandidateConnectionsToEndNode(Int3 position, uint parentPathNode, uint parentNodeIndex, uint parentG)
        {
        }

        /// <summary>
        /// Opens a connection between two nodes during the A* search.
        ///
        /// When a node is "opened" (i.e. searched by the A* algorithm), it will open connections to all its neighbours.
        /// This function checks those connections to see if passing through the node to its neighbour is the best way to reach the neighbour that we have seen so far,
        /// and if so, it will push the neighbour onto the search heap.
        /// </summary>
        /// <param name="parentPathNode">The node that is being opened.</param>
        /// <param name="targetPathNode">A neighbour of the parent that is being considered.</param>
        /// <param name="parentG">The G value of the parent node. This is the cost to reach the parent node from the start of the path.</param>
        /// <param name="connectionCost">The cost of moving from the parent node to the target node.</param>
        /// <param name="fractionAlongEdge">Internal value used by the TriangleMeshNode to store where on the shared edge between the nodes we say we cross over.</param>
        /// <param name="targetNodePosition">The position of the target node. This is used by the heuristic to estimate the cost to reach the end node.</param>
        public void OpenCandidateConnection(uint parentPathNode, uint targetPathNode, uint parentG, uint connectionCost, uint fractionAlongEdge, Int3 targetNodePosition)
        {
        }

        /// <summary>
        /// Parameters to OpenCandidateConnectionBurst.
        /// Using a struct instead of passing the parameters as separate arguments is significantly faster.
        /// </summary>
        public struct OpenCandidateParams
        {
            public UnsafeSpan<PathNode> pathNodes;
            public uint parentPathNode;
            public uint targetPathNode;
            public uint targetNodeIndex;
            public uint candidateG;
            public uint fractionAlongEdge;
            public int3 targetNodePosition;
            public ushort pathID;
        }

        /// <summary>
        /// Burst-compiled internal implementation of OpenCandidateConnection.
        /// Compiling it using burst provides a decent 25% speedup.
        /// The function itself is much faster, but the overhead of calling it from C# is quite significant.
        /// </summary>
        [Unity.Burst.BurstCompile]
        public static void OpenCandidateConnectionBurst(ref OpenCandidateParams pars, ref BinaryHeap heap, ref HeuristicObjective heuristicObjective)
        {
        }

        /// <summary>Returns penalty for the given tag.</summary>
        /// <param name="tag">A value between 0 (inclusive) and 32 (exclusive).</param>
        public uint GetTagPenalty(int tag)
        {
            return default;
        }

        /// <summary>
        /// Returns if the node can be traversed.
        /// This by default equals to if the node is walkable and if the node's tag is included in <see cref="enabledTags"/>.
        ///
        /// See: <see cref="traversalProvider"/>
        /// </summary>
        public bool CanTraverse(GraphNode node)
        {
            return default;
        }


        /// <summary>
        /// Returns if the path can traverse a link between from and to and if to can be traversed itself.
        /// This by default equals to if the to is walkable and if the to's tag is included in <see cref="enabledTags"/>.
        ///
        /// See: <see cref="traversalProvider"/>
        /// </summary>
        public bool CanTraverse(GraphNode from, GraphNode to)
        {
            return default;
        }

        /// <summary>Returns the cost of traversing the given node</summary>
        public uint GetTraversalCost(GraphNode node)
        {
            return default;
        }

        /// <summary>
        /// True if this path is done calculating.
        ///
        /// Note: The callback for the path might not have been called yet.
        ///
        /// See: <see cref="Seeker.IsDone"/> which also takes into account if the path callback has been called and had modifiers applied.
        /// </summary>
        public bool IsDone()
        {
            return default;
        }

        /// <summary>Threadsafe increment of the state</summary>
        void IPathInternals.AdvanceState(PathState s)
        {
        }

        /// <summary>Causes the path to fail and sets <see cref="errorLog"/> to msg</summary>
        public void FailWithError(string msg)
        {
        }

        /// <summary>
        /// Aborts the path because of an error.
        /// Sets <see cref="error"/> to true.
        /// This function is called when an error has occurred (e.g a valid path could not be found).
        /// See: <see cref="FailWithError"/>
        /// </summary>
        public void Error()
        {
        }

        /// <summary>
        /// Performs some error checking.
        /// Makes sure the user isn't using old code paths and that no major errors have been made.
        ///
        /// Causes the path to fail if any errors are found.
        /// </summary>
        private void ErrorCheck()
        {
        }

        /// <summary>
        /// Called when the path enters the pool.
        /// This method should release e.g pooled lists and other pooled resources
        /// The base version of this method releases vectorPath and path lists.
        /// Reset() will be called after this function, not before.
        /// Warning: Do not call this function manually.
        /// </summary>
        protected virtual void OnEnterPool()
        {
        }

        /// <summary>
        /// Reset all values to their default values.
        ///
        /// Note: All inheriting path types (e.g ConstantPath, RandomPath, etc.) which declare their own variables need to
        /// override this function, resetting ALL their variables to enable pooling of paths.
        /// If this is not done, trying to use that path type for pooling could result in weird behaviour.
        /// The best way is to reset to default values the variables declared in the extended path type and then
        /// call the base function in inheriting types with base.Reset().
        /// </summary>
        protected virtual void Reset()
        {
        }

        /// <summary>List of claims on this path with reference objects</summary>
        private List<System.Object> claimed = new List<System.Object>();

        /// <summary>
        /// True if the path has been released with a non-silent call yet.
        ///
        /// See: Release
        /// See: Claim
        /// </summary>
        private bool releasedNotSilent;

        /// <summary>
        /// Increase the reference count on this path by 1 (for pooling).
        /// A claim on a path will ensure that it is not pooled.
        /// If you are using a path, you will want to claim it when you first get it and then release it when you will not
        /// use it anymore. When there are no claims on the path, it will be reset and put in a pool.
        ///
        /// This is essentially just reference counting.
        ///
        /// The object passed to this method is merely used as a way to more easily detect when pooling is not done correctly.
        /// It can be any object, when used from a movement script you can just pass "this". This class will throw an exception
        /// if you try to call Claim on the same path twice with the same object (which is usually not what you want) or
        /// if you try to call Release with an object that has not been used in a Claim call for that path.
        /// The object passed to the Claim method needs to be the same as the one you pass to this method.
        ///
        /// See: Release
        /// See: Pool
        /// See: pooling (view in online documentation for working links)
        /// See: https://en.wikipedia.org/wiki/Reference_counting
        /// </summary>
        public void Claim(System.Object o)
        {
        }

        /// <summary>
        /// Reduces the reference count on the path by 1 (pooling).
        /// Removes the claim on the path by the specified object.
        /// When the reference count reaches zero, the path will be pooled, all variables will be cleared and the path will be put in a pool to be used again.
        /// This is great for performance since fewer allocations are made.
        ///
        /// If the silent parameter is true, this method will remove the claim by the specified object
        /// but the path will not be pooled if the claim count reches zero unless a non-silent Release call has been made earlier.
        /// This is used by the internal pathfinding components such as Seeker and AstarPath so that they will not cause paths to be pooled.
        /// This enables users to skip the claim/release calls if they want without the path being pooled by the Seeker or AstarPath and
        /// thus causing strange bugs.
        ///
        /// See: Claim
        /// See: PathPool
        /// </summary>
        public void Release (System.Object o, bool silent = false) {
        }

        /// <summary>
        /// Traces the calculated path from the end node to the start.
        /// This will build an array (<see cref="path)"/> of the nodes this path will pass through and also set the <see cref="vectorPath"/> array to the <see cref="path"/> arrays positions.
        /// Assumes the <see cref="vectorPath"/> and <see cref="path"/> are empty and not null (which will be the case for a correctly initialized path).
        /// </summary>
        protected virtual void Trace(uint fromPathNodeIndex)
        {
        }

        protected void Trace (uint fromPathNodeIndex, bool reverse)
        {
        }

        /// <summary>
        /// Writes text shared for all overrides of DebugString to the string builder.
        /// See: DebugString
        /// </summary>
        protected void DebugStringPrefix(PathLog logMode, System.Text.StringBuilder text)
        {
        }

        /// <summary>
        /// Writes text shared for all overrides of DebugString to the string builder.
        /// See: DebugString
        /// </summary>
        protected void DebugStringSuffix(PathLog logMode, System.Text.StringBuilder text)
        {
        }

        /// <summary>
        /// Returns a string with information about it.
        /// More information is emitted when logMode == Heavy.
        /// An empty string is returned if logMode == None
        /// or logMode == OnlyErrors and this path did not fail.
        /// </summary>
        protected virtual string DebugString(PathLog logMode)
        {
            return default;
        }

        /// <summary>Calls callback to return the calculated path. See: <see cref="callback"/></summary>
        protected virtual void ReturnPath()
        {
        }

        void InitializeNNConstraint()
        {
        }

        /// <summary>
        /// Closest point and node which is traversable by this path.
        ///
        /// This takes both the NNConstraint and the ITraversalProvider into account.
        /// </summary>
        protected NNInfo GetNearest(Vector3 point)
        {
            return default;
        }

        /// <summary>
        /// Prepares low level path variables for calculation.
        /// Called before a path search will take place.
        /// Always called before the Prepare, Initialize and CalculateStep functions
        /// </summary>
        protected void PrepareBase(PathHandler pathHandler)
        {
        }

        /// <summary>
        /// Called before the path is started.
        /// Called right before Initialize
        /// </summary>
        protected abstract void Prepare();

        /// <summary>
        /// Always called after the path has been calculated.
        /// Guaranteed to be called before other paths have been calculated on
        /// the same thread.
        /// Use for cleaning up things like node tagging and similar.
        /// </summary>
        protected virtual void Cleanup()
        {
        }

        protected int3 FirstTemporaryEndNode()
        {
            return default;
        }

        protected void TemporaryEndNodesBoundingBox(out int3 mn, out int3 mx)
        {
            mn = default(int3);
            mx = default(int3);
        }

        protected void MarkNodesAdjacentToTemporaryEndNodes()
        {
        }

        protected void AddStartNodesToHeap()
        {
        }

        /// <summary>
        /// Called when there are no more nodes to search.
        ///
        /// This may be used to calculate a partial path as a fallback.
        /// </summary>
        protected abstract void OnHeapExhausted();

        /// <summary>
        /// Called when a valid node has been found for the end of the path.
        ///
        /// This function should trace the path back to the start node, and set CompleteState to Complete.
        /// If CompleteState is unchanged, the search will continue.
        /// </summary>
        protected abstract void OnFoundEndNode(uint pathNode, uint hScore, uint gScore);

        /// <summary>
        /// Called for every node that the path visits.
        ///
        /// This is used by path types to check if the target node has been reached, to log debug data, etc.
        /// </summary>
        public virtual void OnVisitNode(uint pathNode, uint hScore, uint gScore)
        {
        }

        /// <summary>
        /// Calculates the path until completed or until the time has passed targetTick.
        /// Usually a check is only done every 500 nodes if the time has passed targetTick.
        /// Time/Ticks are got from System.DateTime.UtcNow.Ticks.
        ///
        /// Basic outline of what the function does for the standard path (Pathfinding.ABPath).
        /// <code>
        /// while the end has not been found and no error has occurred
        /// pop the next node of the heap and set it as current
        /// check if we have reached the end
        /// if so, exit and return the path
        ///
        /// open the current node, i.e loop through its neighbours, mark them as visited and put them on a heap
        ///
        /// check if there are still nodes left to process (or have we searched the whole graph)
        /// if there are none, flag error and exit
        ///
        /// check if the function has exceeded the time limit
        /// if so, return and wait for the function to get called again
        /// </code>
        /// </summary>
        protected virtual void CalculateStep(long targetTick)
        {
        }

        PathHandler IPathInternals.PathHandler { get { return pathHandler; } }
        void IPathInternals.OnEnterPool()
        {
        }

        void IPathInternals.Reset()
        {
        }

        void IPathInternals.ReturnPath()
        {
        }

        void IPathInternals.PrepareBase(PathHandler handler)
        {
        }

        void IPathInternals.Prepare()
        {
        }

        void IPathInternals.Cleanup()
        {
        }

        void IPathInternals.CalculateStep(long targetTick)
        {
        }

        string IPathInternals.DebugString(PathLog logMode)
        {
            return default;
        }
    }

	/// <summary>Used for hiding internal methods of the Path class</summary>
	internal interface IPathInternals {
		PathHandler PathHandler { get; }
		bool Pooled { get; set; }
		void AdvanceState(PathState s);
		void OnEnterPool();
		void Reset();
		void ReturnPath();
		void PrepareBase(PathHandler handler);
		void Prepare();
		void Cleanup();
		void CalculateStep(long targetTick);
		string DebugString(PathLog logMode);
	}
}
