using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Pathfinding.Jobs;
using Unity.Mathematics;

namespace Pathfinding.Jobs {
	public struct JobRaycastAll {
		int maxHits;
		public readonly float minStep;

		NativeArray<RaycastHit> results;
		NativeArray<RaycastHit> semiResults;
		NativeArray<RaycastCommand> commands;
		public PhysicsScene physicsScene;

		[BurstCompile]
		private struct JobCreateCommands : IJobParallelFor {
			public NativeArray<RaycastCommand> commands;
			[ReadOnly]
			public NativeArray<RaycastHit> raycastHits;

			public float minStep;
			public PhysicsScene physicsScene;

			public void Execute (int index) {
            }
        }

		[BurstCompile]
		private struct JobCombineResults : IJob {
			public int maxHits;
			[ReadOnly]
			public NativeArray<RaycastHit> semiResults;
			public NativeArray<RaycastHit> results;

			public void Execute () {
            }
        }

		/// <summary>Jobified version of Physics.RaycastNonAlloc.</summary>
		/// <param name="commands">Array of commands to perform.</param>
		/// <param name="results">Array to store results in.</param>
		/// <param name="physicsScene">PhysicsScene to use for the raycasts. Only used in Unity 2022.2 or later.</param>
		/// <param name="maxHits">Max hits count per command.</param>
		/// <param name="allocator">Allocator to use for the results array.</param>
		/// <param name="dependencyTracker">Tracker to use for dependencies.</param>
		/// <param name="minStep">Minimal distance each Raycast should progress.</param>
		public JobRaycastAll(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> results, PhysicsScene physicsScene, int maxHits, Allocator allocator, JobDependencyTracker dependencyTracker, float minStep = 0.0001f) : this()
        {
        }

        public JobHandle Schedule(JobHandle dependency)
        {
            return default;
        }
    }
}
