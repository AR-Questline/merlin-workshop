using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding {
	using Pathfinding.Jobs;
	using Pathfinding.Pooling;
	using Unity.Jobs;
	using Unity.Profiling;
	using UnityEngine.Assertions;
	using UnityEngine.Profiling;

	/// <summary>
	/// Promise representing a graph update.
	///
	/// This is used internally by the system to represent a graph update.
	/// Generally you shouldn't need to care about it, unless you are implementing your own graph type.
	/// </summary>
	public interface IGraphUpdatePromise {
		/// <summary>
		/// Returns the progress of the update.
		///
		/// This should be a value between 0 and 1.
		/// </summary>
		float Progress => 0.0f;

		/// <summary>
		/// Coroutine to prepare an update asynchronously.
		///
		/// If a JobHandle is returned, it will be awaited before the coroutine is ticked again, and before the <see cref="Apply"/> method is called.
		///
		/// After this coroutine has finished, the <see cref="Apply"/> method will be called.
		///
		/// Note: No changes must be made to the graph in this method. Those should only be done in the <see cref="Apply"/> method.
		///
		/// May return null if no async work is required.
		/// </summary>
		IEnumerator<JobHandle> Prepare() => null;

		/// <summary>
		/// Applies the update in a single atomic update.
		///
		/// It is done as a single atomic update (from the main thread's perspective) to ensure
		/// that even if one does an async scan or update, the graph will always be in a valid state.
		/// This guarantees that things like GetNearest will still work during an async scan.
		///
		/// Warning: Must only be called after the <see cref="Prepare"/> method has finished.
		/// </summary>
		// TODO: Pass in a JobHandle and allow returning a JobHandle?
		void Apply(IGraphUpdateContext context);
	}

	/// <summary>
	/// Helper functions for graph updates.
	///
	/// A context is passed to graphs when they are updated, and to work items when they are executed.
	/// The <see cref="IWorkItemContext"/> interface inherits from this interface.
	/// </summary>
	public interface IGraphUpdateContext {
		/// <summary>
		/// Mark a particular region of the world as having been changed.
		///
		/// This should be used whenever graphs are changed.
		///
		/// This is used to recalculate off-mesh links that touch these bounds, and it will also ensure <see cref="GraphModifier"/> events are callled.
		///
		/// The bounding box should cover the surface of all nodes that have been updated.
		/// It is fine to use a larger bounding box than necessary (even an infinite one), though this may be slower, since more off-mesh links need to be recalculated.
		/// You can even use an infinitely large bounding box if you don't want to bother calculating a more accurate one.
		/// You can also call this multiple times to dirty multiple bounding boxes.
		/// </summary>
		void DirtyBounds(Bounds bounds);
	}

	class GraphUpdateProcessor {
		/// <summary>Holds graphs that can be updated</summary>
		readonly AstarPath astar;

		/// <summary>Used for IsAnyGraphUpdateInProgress</summary>
		bool anyGraphUpdateInProgress;

		/// <summary>
		/// Queue containing all waiting graph update queries. Add to this queue by using \link AddToQueue \endlink.
		/// See: AddToQueue
		/// </summary>
		readonly Queue<GraphUpdateObject> graphUpdateQueue = new Queue<GraphUpdateObject>();
		readonly List<(IGraphUpdatePromise, IEnumerator<JobHandle>)> pendingPromises = new List<(IGraphUpdatePromise, IEnumerator<JobHandle>)>();
		readonly List<GraphUpdateObject> pendingGraphUpdates = new List<GraphUpdateObject>();

		/// <summary>Returns if any graph updates are waiting to be applied</summary>
		public bool IsAnyGraphUpdateQueued { get { return graphUpdateQueue.Count > 0; } }

		/// <summary>Returns if any graph updates are in progress</summary>
		public bool IsAnyGraphUpdateInProgress { get { return anyGraphUpdateInProgress; } }

		public GraphUpdateProcessor (AstarPath astar) {
        }

        /// <summary>Work item which can be used to apply all queued updates</summary>
        public AstarWorkItem GetWorkItem () {
            return default;
        }

        /// <summary>
        /// Update all graphs using the GraphUpdateObject.
        /// This can be used to, e.g make all nodes in an area unwalkable, or set them to a higher penalty.
        /// The graphs will be updated as soon as possible (with respect to AstarPath.batchGraphUpdates)
        ///
        /// See: FlushGraphUpdates
        /// </summary>
        public void AddToQueue(GraphUpdateObject ob)
        {
        }

        /// <summary>
        /// Discards all queued graph updates.
        ///
        /// Graph updates that are already in progress will not be discarded.
        /// </summary>
        public void DiscardQueued()
        {
        }

        /// <summary>Schedules graph updates internally</summary>
        void QueueGraphUpdatesInternal(IWorkItemContext context)
        {
        }

        static readonly ProfilerMarker MarkerSleep = new ProfilerMarker(ProfilerCategory.Loading, "Sleep");
		static readonly ProfilerMarker MarkerCalculate = new ProfilerMarker("Calculating Graph Update");
		static readonly ProfilerMarker MarkerApply = new ProfilerMarker("Applying Graph Update");

		/// <summary>
		/// Updates graphs.
		/// Will do some graph updates, possibly signal another thread to do them.
		/// Will only process graph updates added by QueueGraphUpdatesInternal
		///
		/// Returns: True if all graph updates have been done and pathfinding (or other tasks) may resume.
		/// False if there are still graph updates being processed or waiting in the queue.
		/// </summary>
		/// <param name="context">Helper methods for the work items.</param>
		/// <param name="force">If true, all graph updates will be processed before this function returns. The return value
		/// will be True.</param>
		bool ProcessGraphUpdates (IWorkItemContext context, bool force) {
            return default;
        }

        public static int ProcessGraphUpdatePromises(List<(IGraphUpdatePromise, IEnumerator<JobHandle>)> promises, IGraphUpdateContext context, TimeSlice timeSlice)
        {
            return default;
        }

        public static int PrepareGraphUpdatePromises(List<(IGraphUpdatePromise, IEnumerator<JobHandle>)> promises, TimeSlice timeSlice)
        {
            return default;
        }

        public static void ApplyGraphUpdatePromises(List<(IGraphUpdatePromise, IEnumerator<JobHandle>)> promises, IGraphUpdateContext context)
        {
        }
    }
}
