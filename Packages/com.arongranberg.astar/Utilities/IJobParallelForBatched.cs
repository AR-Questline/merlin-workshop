// This file is only included because the Unity.Jobs package is currently experimental and it seems bad to rely on it.
// The Unity.Jobs version of this interface will be used when it is stable.
using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Pathfinding.Jobs {
	[JobProducerType(typeof(JobParallelForBatchedExtensions.ParallelForBatchJobStruct<>))]
	public interface IJobParallelForBatched {
		bool allowBoundsChecks { get; }
		void Execute(int startIndex, int count);
	}

	static class JobParallelForBatchedExtensions {
		internal struct ParallelForBatchJobStruct<T> where T : struct, IJobParallelForBatched {
			static public IntPtr jobReflectionData;

			public static IntPtr Initialize () {
                return default;
            }

            public delegate void ExecuteJobFunction(ref T data, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public unsafe static void Execute(ref T jobData, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
            }
        }

		unsafe static public JobHandle ScheduleBatch<T>(this T jobData, int arrayLength, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatched {
            return default;
        }

        unsafe static public void RunBatch<T>(this T jobData, int arrayLength) where T : struct, IJobParallelForBatched
        {
        }
    }
}
