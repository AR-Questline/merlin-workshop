// #define DEBUG_RWLOCK
using Unity.Jobs;

namespace Pathfinding.Sync {
	/// <summary>
	/// A simple read/write lock for use with the Unity Job System.
	///
	/// The RW-lock makes the following assumptions:
	/// - Only the main thread will call the methods on this lock.
	/// - If jobs are to use locked data, you should call <see cref="Read"/> or <see cref="Write"/> on the lock and pass the returned JobHandle as a dependency the job, and then call <see cref="WriteLockAsync.UnlockAfter"/> on the lock object, with the newly scheduled job's handle.
	/// - When taking a Read lock, you should only read data, but if you take a Write lock you may modify data.
	/// - On the main thread, multiple synchronous write locks may be nested.
	///
	/// You do not need to care about dependencies when calling the <see cref="ReadSync"/> and <see cref="WriteSync"/> methods. That's handled automatically for you.
	///
	/// See: https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock
	///
	/// <code>
	/// var readLock = AstarPath.active.LockGraphDataForReading();
	/// var handle = new MyJob {
	///     // ...
	/// }.Schedule(readLock.dependency);
	/// readLock.UnlockAfter(handle);
	/// </code>
	/// </summary>
	public class RWLock {
		JobHandle lastWrite;
		JobHandle lastRead;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		int heldSyncLocks;
		bool pendingAsync;
#if DEBUG_RWLOCK
		string pendingStackTrace;
#endif

		void CheckPendingAsync () {
        }
#endif

        void AddPendingSync () {
        }

        void RemovePendingSync () {
        }

        void AddPendingAsync () {
        }

        void RemovePendingAsync()
        {
        }

        /// <summary>
        /// Aquire a read lock on the main thread.
        /// This method will block until all pending write locks have been released.
        /// </summary>
        public LockSync ReadSync()
        {
            return default;
        }

        /// <summary>
        /// Aquire a read lock on the main thread.
        /// This method will not block until all asynchronous write locks have been released, instead you should make sure to add the returned JobHandle as a dependency to any jobs that use the locked data.
        ///
        /// If a synchronous write lock is currently held, this method will throw an exception.
        ///
        /// <code>
        /// var readLock = AstarPath.active.LockGraphDataForReading();
        /// var handle = new MyJob {
        ///     // ...
        /// }.Schedule(readLock.dependency);
        /// readLock.UnlockAfter(handle);
        /// </code>
        /// </summary>
        public ReadLockAsync Read()
        {
            return default;
        }

        /// <summary>
        /// Aquire a write lock on the main thread.
        /// This method will block until all pending read and write locks have been released.
        /// </summary>
        public LockSync WriteSync()
        {
            return default;
        }

        /// <summary>
        /// Aquire a write lock on the main thread.
        /// This method will not block until all asynchronous read and write locks have been released, instead you should make sure to add the returned JobHandle as a dependency to any jobs that use the locked data.
        ///
        /// If a synchronous write lock is currently held, this method will throw an exception.
        ///
        /// <code>
        /// var readLock = AstarPath.active.LockGraphDataForReading();
        /// var handle = new MyJob {
        ///     // ...
        /// }.Schedule(readLock.dependency);
        /// readLock.UnlockAfter(handle);
        /// </code>
        /// </summary>
        public WriteLockAsync Write()
        {
            return default;
        }

        public readonly struct CombinedReadLockAsync {
			readonly RWLock lock1;
			readonly RWLock lock2;
			public readonly JobHandle dependency;

			public CombinedReadLockAsync(ReadLockAsync lock1, ReadLockAsync lock2) : this()
            {
            }

            /// <summary>Release the lock after the given job has completed</summary>
            public readonly void UnlockAfter(JobHandle handle)
            {
            }
        }

		public readonly struct ReadLockAsync {
			internal readonly RWLock inner;
			public readonly JobHandle dependency;

			public ReadLockAsync(RWLock inner, JobHandle dependency) : this()
            {
            }

            /// <summary>Release the lock after the given job has completed</summary>
            public readonly void UnlockAfter(JobHandle handle)
            {
            }
        }

		public readonly struct WriteLockAsync {
			readonly RWLock inner;
			public readonly JobHandle dependency;

			public WriteLockAsync(RWLock inner, JobHandle dependency) : this()
            {
            }

            /// <summary>Release the lock after the given job has completed</summary>
            public readonly void UnlockAfter(JobHandle handle)
            {
            }
        }

		public readonly struct LockSync : System.IDisposable {
			readonly RWLock inner;

			public LockSync(RWLock inner) : this()
            {
            }

            /// <summary>Release the lock</summary>
            public readonly void Unlock () {
            }

            readonly void System.IDisposable.Dispose () {
            }
        }
	}
}
