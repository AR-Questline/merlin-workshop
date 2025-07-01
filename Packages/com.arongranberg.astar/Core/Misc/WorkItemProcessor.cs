using UnityEngine;
using UnityEngine.Profiling;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace Pathfinding {
	/// <summary>
	/// An item of work that can be executed when graphs are safe to update.
	/// See: <see cref="AstarPath.UpdateGraphs"/>
	/// See: <see cref="AstarPath.AddWorkItem"/>
	/// </summary>
	public struct AstarWorkItem {
		/// <summary>
		/// Init function.
		/// May be null if no initialization is needed.
		/// Will be called once, right before the first call to <see cref="update"/> or <see cref="updateWithContext"/>.
		/// </summary>
		public System.Action init;

		/// <summary>
		/// Init function.
		/// May be null if no initialization is needed.
		/// Will be called once, right before the first call to <see cref="update"/> or <see cref="updateWithContext"/>.
		///
		/// A context object is sent as a parameter. This can be used
		/// to for example queue a flood fill that will be executed either
		/// when a work item calls EnsureValidFloodFill or all work items have
		/// been completed. If multiple work items are updating nodes
		/// so that they need a flood fill afterwards, using the QueueFloodFill
		/// method is preferred since then only a single flood fill needs
		/// to be performed for all of the work items instead of one
		/// per work item.
		/// </summary>
		public System.Action<IWorkItemContext> initWithContext;

		/// <summary>
		/// Update function, called once per frame when the work item executes.
		/// Takes a param force. If that is true, the work item should try to complete the whole item in one go instead
		/// of spreading it out over multiple frames.
		///
		/// Warning: If you make modifications to the graphs, they must only be made during the last time the <see cref="update"/> method is called.
		/// Earlier invocations, as well as the <see cref="init"/>/<see cref="initWithContext"/> mehods, are only for pre-calculating information required for the update.
		///
		/// Returns: True when the work item is completed.
		/// </summary>
		public System.Func<bool, bool> update;

		/// <summary>
		/// Update function, called once per frame when the work item executes.
		/// Takes a param force. If that is true, the work item should try to complete the whole item in one go instead
		/// of spreading it out over multiple frames.
		/// Returns: True when the work item is completed.
		///
		/// Warning: If you make modifications to the graphs, they must only be made during the last time the <see cref="update"/> method is called.
		/// Earlier invocations, as well as the <see cref="init"/>/<see cref="initWithContext"/> mehods, are only for pre-calculating information required for the update.
		///
		/// A context object is sent as a parameter. This can be used
		/// to for example queue a flood fill that will be executed either
		/// when a work item calls EnsureValidFloodFill or all work items have
		/// been completed. If multiple work items are updating nodes
		/// so that they need a flood fill afterwards, using the QueueFloodFill
		/// method is preferred since then only a single flood fill needs
		/// to be performed for all of the work items instead of one
		/// per work item.
		/// </summary>
		public System.Func<IWorkItemContext, bool, bool> updateWithContext;

		/// <summary>Creates a work item which will call the specified functions when executed.</summary>
		/// <param name="update">Will be called once per frame when the work item executes. See #update for details.</param>
		public AstarWorkItem (System.Func<bool, bool> update) : this()
        {
        }

        /// <summary>Creates a work item which will call the specified functions when executed.</summary>
        /// <param name="update">Will be called once per frame when the work item executes. See #updateWithContext for details.</param>
        public AstarWorkItem(System.Func<IWorkItemContext, bool, bool> update) : this()
        {
        }

        /// <summary>Creates a work item which will call the specified functions when executed.</summary>
        /// <param name="init">Will be called once, right before the first call to update. See #init for details.</param>
        /// <param name="update">Will be called once per frame when the work item executes. See #update for details.</param>
        public AstarWorkItem(System.Action init, System.Func<bool, bool> update = null) : this()
        {
        }

        /// <summary>Creates a work item which will call the specified functions when executed.</summary>
        /// <param name="init">Will be called once, right before the first call to update. See #initWithContext for details.</param>
        /// <param name="update">Will be called once per frame when the work item executes. See #updateWithContext for details.</param>
        public AstarWorkItem(System.Action<IWorkItemContext> init, System.Func<IWorkItemContext, bool, bool> update = null) : this()
        {
        }
    }

	/// <summary>Interface to expose a subset of the WorkItemProcessor functionality</summary>
	public interface IWorkItemContext : IGraphUpdateContext {
		/// <summary>
		/// Call during work items to queue a flood fill.
		/// An instant flood fill can be done via FloodFill()
		/// but this method can be used to batch several updates into one
		/// to increase performance.
		/// WorkItems which require a valid Flood Fill in their execution can call EnsureValidFloodFill
		/// to ensure that a flood fill is done if any earlier work items queued one.
		///
		/// Once a flood fill is queued it will be done after all WorkItems have been executed.
		///
		/// Deprecated: You no longer need to call this method. Connectivity data is automatically kept up-to-date.
		/// </summary>
		[System.Obsolete("You no longer need to call this method. Connectivity data is automatically kept up-to-date.")]
		void QueueFloodFill();

		/// <summary>
		/// If a WorkItem needs to have a valid area information during execution, call this method to ensure there are no pending flood fills.
		/// If you are using the <see cref="GraphNode.Area"/> property or the <see cref="PathUtilities.IsPathPossible"/> method in your work items, then you may want to call this method before you use them,
		/// to ensure that the data is up to date.
		///
		/// See: <see cref="HierarchicalGraph"/>
		///
		/// <code>
		/// AstarPath.active.AddWorkItem(new AstarWorkItem((IWorkItemContext ctx) => {
		///     // Update the graph in some way
		///     // ...
		///
		///     // Ensure that connectivity information is up to date.
		///     // This will also automatically run after all work items have been executed.
		///     ctx.EnsureValidFloodFill();
		///
		///     // Use connectivity information
		///     if (PathUtilities.IsPathPossible(someNode, someOtherNode)) {
		///         // Do something
		///     }
		/// }));
		/// </code>
		/// </summary>
		void EnsureValidFloodFill();

		/// <summary>
		/// Call to send a GraphModifier.EventType.PreUpdate event to all graph modifiers.
		/// The difference between this and GraphModifier.TriggerEvent(GraphModifier.EventType.PreUpdate) is that using this method
		/// ensures that multiple PreUpdate events will not be issued during a single update.
		///
		/// Once an event has been sent no more events will be sent until all work items are complete and a PostUpdate or PostScan event is sent.
		///
		/// When scanning a graph PreUpdate events are never sent. However a PreScan event is always sent before a scan begins.
		/// </summary>
		void PreUpdate();

		/// <summary>
		/// Trigger a graph modification event.
		/// This will cause a <see cref="GraphModifier.EventType.PostUpdate"/> event to be issued after all graph updates have finished.
		/// Some scripts listen for this event. For example off-mesh links listen to it and will recalculate which nodes they are connected to when it it sent.
		/// If a graph is dirtied multiple times, or even if multiple graphs are dirtied, the event will only be sent once.
		/// </summary>
		// TODO: Deprecate?
		void SetGraphDirty(NavGraph graph);
	}

	class WorkItemProcessor : IWorkItemContext {
		public event System.Action OnGraphsUpdated;

		/// <summary>Used to prevent waiting for work items to complete inside other work items as that will cause the program to hang</summary>
		public bool workItemsInProgressRightNow { get; private set; }

		readonly AstarPath astar;
		readonly IndexedQueue<AstarWorkItem> workItems = new IndexedQueue<AstarWorkItem>();


		/// <summary>True if any work items are queued right now</summary>
		public bool anyQueued {
			get { return workItems.Count > 0; }
		}

		bool anyGraphsDirty = true;
		bool preUpdateEventSent = false;

		/// <summary>
		/// True while a batch of work items are being processed.
		/// Set to true when a work item is started to be processed, reset to false when all work items are complete.
		///
		/// Work item updates are often spread out over several frames, this flag will be true during the whole time the
		/// updates are in progress.
		/// </summary>
		public bool workItemsInProgress { get; private set; }

		/// <summary>Similar to Queue<T> but allows random access</summary>
		// TODO: Replace with CircularBuffer?
		class IndexedQueue<T> {
			T[] buffer = new T[4];
			int start;

			public T this[int index] {
				get {
					if (index < 0 || index >= Count) throw new System.IndexOutOfRangeException();
					return buffer[(start + index) % buffer.Length];
				}
				set {
					if (index < 0 || index >= Count) throw new System.IndexOutOfRangeException();
					buffer[(start + index) % buffer.Length] = value;
				}
			}

			public int Count { get; private set; }

			public void Enqueue (T item) {
            }

            public T Dequeue () {
                return default;
            }
        }

        /// <summary>
        /// Call during work items to queue a flood fill.
        /// An instant flood fill can be done via FloodFill()
        /// but this method can be used to batch several updates into one
        /// to increase performance.
        /// WorkItems which require a valid Flood Fill in their execution can call EnsureValidFloodFill
        /// to ensure that a flood fill is done if any earlier work items queued one.
        ///
        /// Once a flood fill is queued it will be done after all WorkItems have been executed.
        ///
        /// Deprecated: This method no longer does anything.
        /// </summary>
        void IWorkItemContext.QueueFloodFill()
        {
        }

        void IWorkItemContext.PreUpdate()
        {
        }

        // This will also call DirtyGraphs
        void IWorkItemContext.SetGraphDirty(NavGraph graph) => astar.DirtyBounds(graph.bounds);

		// This will also call DirtyGraphs
		void IGraphUpdateContext.DirtyBounds(Bounds bounds) => astar.DirtyBounds(bounds);

		internal void DirtyGraphs () {
        }

        /// <summary>If a WorkItem needs to have a valid area information during execution, call this method to ensure there are no pending flood fills</summary>
        public void EnsureValidFloodFill () {
        }

        public WorkItemProcessor (AstarPath astar) {
        }

        /// <summary>
        /// Add a work item to be processed when pathfinding is paused.
        ///
        /// See: ProcessWorkItems
        /// </summary>
        public void AddWorkItem (AstarWorkItem item) {
        }

        bool ProcessWorkItems (bool force, bool sendEvents) {
            return default;
        }

        /// <summary>
        /// Process graph updating work items.
        /// Process all queued work items, e.g graph updates and the likes.
        ///
        /// Returns:
        /// - false if there are still items to be processed.
        /// - true if the last work items was processed and pathfinding threads are ready to be resumed.
        ///
        /// This will not call <see cref="EnsureValidFloodFill"/>	in contrast to <see cref="ProcessWorkItemsForUpdate"/>.
        ///
        /// See: <see cref="AstarPath.AddWorkItem"/>
        /// </summary>
        public bool ProcessWorkItemsForScan(bool force)
        {
            return default;
        }

        /// <summary>
        /// Process graph updating work items.
        /// Process all queued work items, e.g graph updates and the likes.
        ///
        /// Returns:
        /// - false if there are still items to be processed.
        /// - true if the last work items was processed and pathfinding threads are ready to be resumed.
        ///
        /// See: <see cref="AstarPath.AddWorkItem"/>
        ///
        /// This method also calls GraphModifier.TriggerEvent(PostUpdate) if any graphs were dirtied.
        /// It also calls <see cref="EnsureValidFloodFill"/> after the work items are done
        /// </summary>
        public bool ProcessWorkItemsForUpdate(bool force)
        {
            return default;
        }
    }
}
