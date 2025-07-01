using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Profiling;
using UnityEngine.Assertions;
using Pathfinding.Sync;

namespace Pathfinding {
#if NETFX_CORE
	using Thread = Pathfinding.WindowsStore.Thread;
#else
	using Thread = System.Threading.Thread;
#endif

	public class PathProcessor {
		public event System.Action<Path> OnPathPreSearch;
		public event System.Action<Path> OnPathPostSearch;
		public event System.Action OnQueueUnblocked;

		internal BlockableChannel<Path> queue;
		readonly AstarPath astar;
		readonly PathReturnQueue returnQueue;

		PathHandler[] pathHandlers;

		/// <summary>References to each of the pathfinding threads</summary>
		Thread[] threads;
		bool multithreaded;

		/// <summary>
		/// When no multithreading is used, the IEnumerator is stored here.
		/// When no multithreading is used, a coroutine is used instead. It is not directly called with StartCoroutine
		/// but a separate function has just a while loop which increments the main IEnumerator.
		/// This is done so other functions can step the thread forward at any time, without having to wait for Unity to update it.
		/// See: <see cref="CalculatePaths"/>
		/// See: <see cref="CalculatePathsThreaded"/>
		/// </summary>
		IEnumerator threadCoroutine;
		BlockableChannel<Path>.Receiver coroutineReceiver;

		readonly List<int> locks = new List<int>();
		int nextLockID = 0;

		static readonly Unity.Profiling.ProfilerMarker MarkerCalculatePath = new Unity.Profiling.ProfilerMarker("Calculating Path");
		static readonly Unity.Profiling.ProfilerMarker MarkerPreparePath = new Unity.Profiling.ProfilerMarker("Prepare Path");

		/// <summary>
		/// Number of parallel pathfinders.
		/// Returns the number of concurrent processes which can calculate paths at once.
		/// When using multithreading, this will be the number of threads, if not using multithreading it is always 1 (since only 1 coroutine is used).
		/// See: threadInfos
		/// See: IsUsingMultithreading
		/// </summary>
		public int NumThreads {
			get {
				return pathHandlers.Length;
			}
		}

		/// <summary>Returns whether or not multithreading is used</summary>
		public bool IsUsingMultithreading {
			get {
				return multithreaded;
			}
		}

		internal PathProcessor (AstarPath astar, PathReturnQueue returnQueue, int processors, bool multithreaded)
        {
        }

        /// <summary>
        /// Changes the number of threads used for pathfinding.
        ///
        /// If multithreading is disabled, processors must be equal to 1.
        /// </summary>
        public void SetThreadCount(int processors, bool multithreaded)
        {
        }

        void StartThreads()
        {
        }

        /// <summary>Prevents pathfinding from running while held</summary>
        public struct GraphUpdateLock : System.IDisposable
        {
            PathProcessor pathProcessor;
			int id;

			public GraphUpdateLock (PathProcessor pathProcessor, bool block) : this()
            {
            }

            /// <summary>
            /// True while this lock is preventing the pathfinding threads from processing more paths.
            /// Note that the pathfinding threads may not be paused yet (if this lock was obtained using PausePathfinding(false)).
            /// </summary>
            public bool Held => pathProcessor != null && pathProcessor.locks.Contains(id);

            /// <summary>Allow pathfinding to start running again if no other locks are still held</summary>
            public void Release() => pathProcessor.Unlock(id);

            void System.IDisposable.Dispose()
            {
            }
        }

        int Lock(bool block)
        {
            return default;
        }

        void Unlock(int id)
        {
        }

        /// <summary>
        /// Prevents pathfinding threads from starting to calculate any new paths.
        ///
        /// Returns: A lock object. You need to call Unlock on that object to allow pathfinding to resume.
        ///
        /// Note: In most cases this should not be called from user code.
        /// </summary>
        /// <param name="block">If true, this call will block until all pathfinding threads are paused.
        /// otherwise the threads will be paused as soon as they are done with what they are currently doing.</param>
        public GraphUpdateLock PausePathfinding(bool block)
        {
            return default;
        }

        /// <summary>
        /// Does pathfinding calculations when not using multithreading.
        ///
        /// This method should be called once per frame if <see cref="IsUsingMultithreading"/> is true.
        /// </summary>
        public void TickNonMultithreaded()
        {
        }

        /// <summary>
        /// Calls 'Join' on each of the threads to block until they have completed.
        ///
        /// This will also clean up any unmanaged memory used by the threads.
        /// </summary>
        public void StopThreads()
        {
        }

        /// <summary>
        /// Cleans up all native memory managed by this instance.
        ///
        /// You may use this instance again by calling SetThreadCount.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Main pathfinding method (multithreaded).
        /// This method will calculate the paths in the pathfinding queue when multithreading is enabled.
        ///
        /// See: CalculatePaths
        /// See: <see cref="AstarPath.StartPath"/>
        /// </summary>
        void CalculatePathsThreaded(PathHandler pathHandler, BlockableChannel<Path>.Receiver receiver)
        {
        }

        /// <summary>
        /// Main pathfinding method.
        /// This method will calculate the paths in the pathfinding queue.
        ///
        /// See: CalculatePathsThreaded
        /// See: StartPath
        /// </summary>
        IEnumerator CalculatePaths(PathHandler pathHandler)
        {
            return default;
        }
    }
}
