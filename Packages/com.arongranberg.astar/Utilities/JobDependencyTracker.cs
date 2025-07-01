// #define DEBUG_JOBS
namespace Pathfinding.Jobs {
	using System.Reflection;
	using Unity.Collections;
	using Unity.Jobs;
	using System.Collections.Generic;
	using Unity.Collections.LowLevel.Unsafe;
	using Pathfinding.Pooling;
	using Pathfinding.Collections;
	using System.Runtime.InteropServices;
	using System.Diagnostics;

	/// <summary>
	/// Disable the check that prevents jobs from including uninitialized native arrays open for reading.
	///
	/// Sometimes jobs have to include a readable native array that starts out uninitialized.
	/// The job might for example write to it and later read from it in the same job.
	///
	/// See: <see cref="JobDependencyTracker.NewNativeArray"/>
	/// </summary>
	class DisableUninitializedReadCheckAttribute : System.Attribute {
	}

	public interface IArenaDisposable {
		void DisposeWith(DisposeArena arena);
	}

	/// <summary>Convenient collection of items that can be disposed together</summary>
	public class DisposeArena {
		List<NativeArray<byte> > buffer;
		List<NativeList<byte> > buffer2;
		List<NativeQueue<byte> > buffer3;
		List<GCHandle> gcHandles;

		public void Add<T>(NativeArray<T> data) where T : unmanaged {
        }

        public void Add<T>(NativeList<T> data) where T : unmanaged {
        }

        public void Add<T>(NativeQueue<T> data) where T : unmanaged {
        }

        public void Remove<T>(NativeArray<T> data) where T : unmanaged
        {
        }

        public void Add<T>(T data) where T : IArenaDisposable
        {
        }

        public void Add(GCHandle handle)
        {
        }

        /// <summary>
        /// Dispose all items in the arena.
        /// This also clears the arena and makes it available for reuse.
        /// </summary>
        public void DisposeAll()
        {
        }
    }

	// TODO: Remove or use?
	public struct JobHandleWithMainThreadWork<T> where T : struct {
		JobDependencyTracker tracker;
		IEnumerator<(JobHandle, T)> coroutine;

		public JobHandleWithMainThreadWork (IEnumerator<(JobHandle, T)> handles, JobDependencyTracker tracker) : this()
        {
        }

        public void Complete()
        {
        }

        public System.Collections.Generic.IEnumerable<T?> CompleteTimeSliced(float maxMillisPerStep)
        {
            return default;
        }
    }

	enum LinearDependencies : byte {
		Check,
		Enabled,
		Disabled,
	}

	/// <summary>
	/// Automatic dependency tracking for the Unity Job System.
	///
	/// Uses reflection to find the [ReadOnly] and [WriteOnly] attributes on job data struct fields.
	/// These are used to automatically figure out dependencies between jobs.
	///
	/// A job that reads from an array depends on the last job that wrote to that array.
	/// A job that writes to an array depends on the last job that wrote to the array as well as all jobs that read from the array.
	///
	/// <code>
	/// struct ExampleJob : IJob {
	///     public NativeArray<int> someData;
	///
	///     public void Execute () {
	///         // Do something
	///     }
	/// }
	///
	/// void Start () {
	///     var tracker = new JobDependencyTracker();
	///     var data = new NativeArray<int>(100, Allocator.TempJob);
	///     var job1 = new ExampleJob {
	///         someData = data
	///     }.Schedule(tracker);
	///
	///     var job2 = new ExampleJob {
	///         someData = data
	///     }.Schedule(tracker);
	///
	///     // job2 automatically depends on job1 because they both require read/write access to the data array
	/// }
	/// </code>
	///
	/// See: <see cref="IJobExtensions"/>
	/// </summary>
	public class JobDependencyTracker : IAstarPooledObject {
		internal List<NativeArraySlot> slots = ListPool<NativeArraySlot>.Claim();
		DisposeArena arena;
		internal NativeArray<JobHandle> dependenciesScratchBuffer;
		LinearDependencies linearDependencies;
		internal TimeSlice timeSlice = TimeSlice.Infinite;


#if ENABLE_UNITY_COLLECTIONS_CHECKS
		~JobDependencyTracker() {
        }
#endif

        public bool forceLinearDependencies {
			get {
				if (linearDependencies == LinearDependencies.Check) SetLinearDependencies(false);
				return linearDependencies == LinearDependencies.Enabled;
			}
		}

		internal struct JobInstance {
			public JobHandle handle;
			public int hash;
#if DEBUG_JOBS
			public string name;
#endif
		}

		internal struct NativeArraySlot {
			public long hash;
			public JobInstance lastWrite;
			public List<JobInstance> lastReads;
			public bool initialized;
			public bool hasWrite;
		}

		// Note: burst compiling even an empty job can avoid the overhead of going from unmanaged to managed code.
		/* [BurstCompile]
		struct JobDispose<T> : IJob where T : struct {
		    [DeallocateOnJobCompletion]
		    [DisableUninitializedReadCheck]
		    public NativeArray<T> data;

		    public void Execute () {
		    }
		}*/

		struct JobRaycastCommandDummy : IJob {
			[ReadOnly]
			public NativeArray<UnityEngine.RaycastCommand> commands;
			[WriteOnly]
			public NativeArray<UnityEngine.RaycastHit> results;

			public void Execute () {}
        }

#if UNITY_2022_2_OR_NEWER
		struct JobOverlapCapsuleCommandDummy : IJob {
			[ReadOnly]
			public NativeArray<UnityEngine.OverlapCapsuleCommand> commands;
			[WriteOnly]
			public NativeArray<UnityEngine.ColliderHit> results;

			public void Execute () {}
        }

		struct JobOverlapSphereCommandDummy : IJob {
			[ReadOnly]
			public NativeArray<UnityEngine.OverlapSphereCommand> commands;
			[WriteOnly]
			public NativeArray<UnityEngine.ColliderHit> results;

			public void Execute () {}
        }
#endif

		/// <summary>
		/// JobHandle that represents a dependency for all jobs.
		/// All native arrays that are written (and have been tracked by this tracker) to will have their final results in them
		/// when the returned job handle is complete.
		///
		/// Warning: Even though all dependencies are complete, the returned JobHandle's IsCompleted property may still return false.
		/// This seems to be a Unity bug (or maybe its by design?).
		/// </summary>
		public JobHandle AllWritesDependency {
			get {
				var handles = new NativeArray<JobHandle>(slots.Count, Allocator.Temp);
				for (int i = 0; i < slots.Count; i++) handles[i] = slots[i].lastWrite.handle;
				var dependencies = JobHandle.CombineDependencies(handles);
				handles.Dispose();
				return dependencies;
			}
		}

		bool supportsMultithreading {
			get {
#if UNITY_WEBGL
				return false;
#else
				return Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount > 0;
#endif
			}
		}

		/// <summary>
		/// Disable dependency tracking and just run jobs one after the other.
		/// This may be faster in some cases since dependency tracking has some overhead.
		/// </summary>
		public void SetLinearDependencies (bool linearDependencies) {
        }

        public NativeArray<T> NewNativeArray<T>(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged {
            return default;
        }

        public void Track<T>(NativeArray<T> array, bool initialized = true) where T : unmanaged
        {
        }

        /// <summary>
        /// Makes the given array not be disposed when this tracker is disposed.
        /// This is useful if you want to keep the array around after the tracker has been disposed.
        /// The array will still be tracked for the purposes of automatic dependency management.
        /// </summary>
        public void Persist<T>(NativeArray<T> array) where T : unmanaged
        {
        }

        /// <summary>
        /// Schedules a raycast batch command.
        /// Like RaycastCommand.ScheduleBatch, but dependencies are tracked automatically.
        /// </summary>
        public JobHandle ScheduleBatch (NativeArray<UnityEngine.RaycastCommand> commands, NativeArray<UnityEngine.RaycastHit> results, int minCommandsPerJob) {
            return default;
        }

#if UNITY_2022_2_OR_NEWER
        /// <summary>
        /// Schedules an overlap capsule batch command.
        /// Like OverlapCapsuleCommand.ScheduleBatch, but dependencies are tracked automatically.
        /// </summary>
        public JobHandle ScheduleBatch (NativeArray<UnityEngine.OverlapCapsuleCommand> commands, NativeArray<UnityEngine.ColliderHit> results, int minCommandsPerJob) {
            return default;
        }

        /// <summary>
        /// Schedules an overlap sphere batch command.
        /// Like OverlapSphereCommand.ScheduleBatch, but dependencies are tracked automatically.
        /// </summary>
        public JobHandle ScheduleBatch (NativeArray<UnityEngine.OverlapSphereCommand> commands, NativeArray<UnityEngine.ColliderHit> results, int minCommandsPerJob) {
            return default;
        }
#endif

        /// <summary>Frees the GCHandle when the JobDependencyTracker is disposed</summary>
        public void DeferFree(GCHandle handle, JobHandle dependsOn)
        {
        }

#if DEBUG_JOBS
		internal void JobReadsFrom (JobHandle job, long nativeArrayHash, int jobHash, string jobName)
#else
        internal void JobReadsFrom(JobHandle job, long nativeArrayHash, int jobHash)
#endif
        {
        }

#if DEBUG_JOBS
		internal void JobWritesTo (JobHandle job, long nativeArrayHash, int jobHash, string jobName)
#else
        internal void JobWritesTo(JobHandle job, long nativeArrayHash, int jobHash)
#endif
        {
        }

        /// <summary>
        /// Disposes this tracker.
        /// This will pool all used lists which makes the GC happy.
        ///
        /// Note: It is necessary to call this method to avoid memory leaks if you are using the DeferDispose method. But it's a good thing to do otherwise as well.
        /// It is automatically called if you are using the ObjectPool<T>.Release method.
        /// </summary>
        void Dispose()
        {
        }

        public void ClearMemory()
        {
        }

        void IAstarPooledObject.OnEnterPool()
        {
        }
    }

	public struct TimeSlice {
		public long endTick;
		public static readonly TimeSlice Infinite = new TimeSlice { endTick = long.MaxValue };
		public bool isInfinite => endTick == long.MaxValue;
		public bool expired => Stopwatch.GetTimestamp() > endTick;

		public static TimeSlice MillisFromNow (float millis) => new TimeSlice { endTick = Stopwatch.GetTimestamp() + (long)(millis * 10000) };
	}

	public interface IJobTimeSliced : IJob {
		/// <summary>
		/// Returns true if the job completed.
		/// If false is returned this job may be called again until the job completes.
		/// </summary>
		bool Execute(TimeSlice timeSlice);
	}

	/// <summary>Extension methods for IJob and related interfaces</summary>
	public static class IJobExtensions {
		struct ManagedJob : IJob {
			public GCHandle handle;

			public void Execute () {
            }
        }

		struct ManagedActionJob : IJob {
			public GCHandle handle;

			public void Execute () {
            }
        }

		/// <summary>
		/// Schedule a job with automatic dependency tracking.
		/// You need to have "using Pathfinding.Util" in your script to be able to use this extension method.
		///
		/// See: <see cref="JobDependencyTracker"/>
		/// </summary>
		// TODO: Compare performance impact by using ref this, and ScheduleByRef
		public static JobHandle Schedule<T>(this T data, JobDependencyTracker tracker) where T : struct, IJob {
            return default;
        }

        /// <summary>Schedules an <see cref="IJobParallelForBatched"/> job with automatic dependency tracking</summary>
        public static JobHandle ScheduleBatch<T>(this T data, int arrayLength, int minIndicesPerJobCount, JobDependencyTracker tracker, JobHandle additionalDependency = default) where T : struct, IJobParallelForBatched
        {
            return default;
        }

        /// <summary>Schedules a managed job to run in the job system</summary>
        public static JobHandle ScheduleManaged<T>(this T data, JobHandle dependsOn) where T : struct, IJob
        {
            return default;
        }

        /// <summary>Schedules a managed job to run in the job system</summary>
        public static JobHandle ScheduleManaged(this System.Action data, JobHandle dependsOn)
        {
            return default;
        }

        public static JobHandle GetDependencies<T>(this T data, JobDependencyTracker tracker) where T : struct, IJob
        {
            return default;
        }

        /// <summary>
        /// Executes this job in the main thread using a coroutine.
        /// Usage:
        /// - 1. Optionally schedule some other jobs before this one (using the dependency tracker)
        /// - 2. Call job.ExecuteMainThreadJob(tracker)
        /// - 3. Iterate over the enumerator until it is finished. Call handle.Complete on all yielded job handles. Usually this only yields once, but if you use the <see cref="JobHandleWithMainThreadWork"/> wrapper it will
        ///    yield once for every time slice.
        /// - 4. Continue scheduling other jobs.
        ///
        /// You must not schedule other jobs (that may touch the same data) while executing this job.
        ///
        /// See: <see cref="JobHandleWithMainThreadWork"/>
        /// </summary>
        public static IEnumerator<JobHandle> ExecuteMainThreadJob<T>(this T data, JobDependencyTracker tracker) where T : struct, IJobTimeSliced
        {
            return default;
        }
    }

	static class JobDependencyAnalyzerAssociated {
		internal static int[] tempJobDependencyHashes = new int[16];
		internal static int jobCounter = 1;
	}

	struct JobDependencyAnalyzer<T> where T : struct {
		static ReflectionData reflectionData;

		/// <summary>Offset to the m_Buffer field inside each NativeArray<T></summary>
		// Note: Due to a Unity bug we have to calculate this for NativeArray<int> instead of NativeArray<>. NativeArray<> will return an incorrect value (-16) when using IL2CPP.
		static readonly int BufferOffset = UnsafeUtility.GetFieldOffset(typeof(NativeArray<int>).GetField("m_Buffer", BindingFlags.Instance | BindingFlags.NonPublic));
		static readonly int SpanPtrOffset = UnsafeUtility.GetFieldOffset(typeof(UnsafeSpan<int>).GetField("ptr", BindingFlags.Instance | BindingFlags.NonPublic));
		struct ReflectionData {
			public int[] fieldOffsets;
			public bool[] writes;
			public bool[] checkUninitializedRead;
			public string[] fieldNames;

			public void Build () {
            }

            void Build(System.Type type, List<int> fields, List<bool> writes, List<bool> reads, List<string> names, int offset, bool forceReadOnly, bool forceWriteOnly, bool forceDisableUninitializedCheck)
            {
            }
        }

        static void initReflectionData()
        {
        }

        static bool HasHash(int[] hashes, int hash, int count)
        {
            return default;
        }

        /// <summary>Returns the dependencies for the given job.</summary>
        /// <param name="data">Job data. Must be allocated on the stack.</param>
        /// <param name="tracker">The tracker to use for dependency tracking.</param>
        public static JobHandle GetDependencies(ref T data, JobDependencyTracker tracker)
        {
            return default;
        }

        public static JobHandle GetDependencies(ref T data, JobDependencyTracker tracker, JobHandle additionalDependency)
        {
            return default;
        }

        static JobHandle GetDependencies(ref T data, JobDependencyTracker tracker, JobHandle additionalDependency, bool useAdditionalDependency)
        {
            return default;
        }

        internal static void Scheduled(ref T data, JobDependencyTracker tracker, JobHandle job)
        {
        }
    }
}
