using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace Pathfinding.RVO {
	/// <summary>
	/// Controls if the agent slows down to a stop if the area around the destination is crowded.
	/// The main idea for this script is to
	/// - Reduce the local avoidance priority for agents that have reached their destination once.
	/// - Make agents stop if there is a high density of units around its destination.
	///
	/// 'High density' is defined as:
	/// Take the circle with the center at the AI's destination and a radius such that the AI's current position
	/// is touching its border. Let 'A' be the area of that circle. Further let 'a' be the total area of all
	/// individual agents inside that circle.
	/// The agent should stop if a > A*0.6 or something like that. I.e if the agents inside the circle cover
	/// over 60% of the surface of the circle. The 60% figure can be modified (see <see cref="densityThreshold)"/>.
	///
	/// This script was inspired by how Starcraft 2 does its local avoidance.
	///
	/// See: <see cref="Pathfinding.AIBase.rvoDensityBehavior"/>
	/// </summary>
	[System.Serializable]
	public struct RVODestinationCrowdedBehavior {
		/// <summary>Enables or disables this module</summary>
		public bool enabled;

		/// <summary>
		/// The threshold for when to stop.
		/// See the class description for more info.
		/// </summary>
		[Range(0, 1)]
		public float densityThreshold;

		/// <summary>
		/// If true, the agent will start to move to the destination again if it determines that it is now less crowded.
		/// If false and the destination becomes less crowded (or if the agent is pushed away from the destination in some way), then the agent will still stay put.
		/// </summary>
		public bool returnAfterBeingPushedAway;

		public float progressAverage;
		bool wasEnabled;
		float timer1;
		float shouldStopDelayTimer;
		bool lastShouldStopResult;
		Vector3 lastShouldStopDestination;
		Vector3 reachedDestinationPoint;
		public bool lastJobDensityResult;

		/// <summary>See https://en.wikipedia.org/wiki/Circle_packing</summary>
		const float MaximumCirclePackingDensity = 0.9069f;

		[BurstCompile(CompileSynchronously = false, FloatMode = FloatMode.Fast)]
		public struct JobDensityCheck : Pathfinding.Jobs.IJobParallelForBatched {
			[ReadOnly]
			RVOQuadtreeBurst quadtree;
			[ReadOnly]
			public NativeArray<QueryData> data;
			[ReadOnly]
			public NativeArray<float3> agentPosition;
			[ReadOnly]
			NativeArray<float3> agentTargetPoint;
			[ReadOnly]
			NativeArray<float> agentRadius;
			[ReadOnly]
			NativeArray<float> agentDesiredSpeed;
			[ReadOnly]
			NativeArray<float3> agentOutputTargetPoint;
			[ReadOnly]
			NativeArray<float> agentOutputSpeed;
			[WriteOnly]
			public NativeArray<bool> outThresholdResult;
			public NativeArray<float> progressAverage;

			public float deltaTime;

			public bool allowBoundsChecks => false;

			public struct QueryData {
				public float3 agentDestination;
				public int agentIndex;
				public float densityThreshold;
			}

			public JobDensityCheck(int size, float deltaTime, SimulatorBurst simulator) : this()
            {
            }

            public void Dispose()
            {
            }

            public void Set(int index, int rvoAgentIndex, float3 destination, float densityThreshold, float progressAverage)
            {
            }

            void Pathfinding.Jobs.IJobParallelForBatched.Execute(int start, int count)
            {
            }

            float AgentDensityInCircle(float3 position, float radius)
            {
                return default;
            }

            void Execute(int i)
            {
            }
        }

		public void ReadJobResult (ref JobDensityCheck jobResult, int index) {
        }

        public RVODestinationCrowdedBehavior (bool enabled, float densityFraction, bool returnAfterBeingPushedAway) : this()
        {
        }

        /// <summary>
        /// Marks the destination as no longer being reached.
        ///
        /// If the agent had stopped because the destination was crowded, this will make it immediately try again
        /// to move forwards	if it can. If the destination is still crowded it will soon stop again.
        ///
        /// This is useful to call when a user gave an agent an explicit order to ensure it doesn't
        /// just stay in the same location without even trying to move forwards.
        /// </summary>
        public void ClearDestinationReached()
        {
        }

        public void OnDestinationChanged(Vector3 newDestination, bool reachedDestination)
        {
        }

        /// <summary>
        /// True if the agent has reached its destination.
        /// If the agents destination changes this may return false until the next frame.
        /// Note that changing the destination every frame may cause this value to never return true.
        ///
        /// True will be returned if the agent has stopped due to being close enough to the destination.
        /// This may be quite some distance away if there are many other agents around the destination.
        ///
        /// See: <see cref="Pathfinding.IAstarAI.destination"/>
        /// </summary>
        public bool reachedDestination { get; private set; }

		bool wasStopped;

		const float DefaultPriority = 1.0f;
		const float StoppedPriority = 0.1f;
		const float MoveBackPriority = 0.5f;

		public void Update (bool rvoControllerEnabled, bool reachedDestination, ref bool isStopped, ref float rvoPriorityMultiplier, ref float rvoFlowFollowingStrength, Vector3 agentPosition) {
        }
    }
}
