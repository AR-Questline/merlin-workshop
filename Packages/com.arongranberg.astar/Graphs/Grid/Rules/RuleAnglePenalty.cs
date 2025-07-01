namespace Pathfinding.Graphs.Grid.Rules {
	using Pathfinding.Jobs;
	using Unity.Jobs;
	using Unity.Collections;
	using Unity.Burst;
	using UnityEngine;
	using Unity.Mathematics;

	/// <summary>
	/// Applies penalty based on the slope of the surface below the node.
	///
	/// This is useful if you for example want to discourage agents from walking on steep slopes.
	///
	/// The penalty applied is equivalent to:
	///
	/// <code>
	/// penalty = curve.evaluate(slope angle in degrees) * penaltyScale
	/// </code>
	///
	/// [Open online documentation to see images]
	///
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[Pathfinding.Util.Preserve]
	public class RuleAnglePenalty : GridGraphRule {
		public float penaltyScale = 10000;
		public AnimationCurve curve = AnimationCurve.Linear(0, 0, 90, 1);
		NativeArray<float> angleToPenalty;

		public override void Register (GridGraphRules rules) {
        }

        public override void DisposeUnmanagedData()
        {
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
		public struct JobPenaltyAngle : IJob {
			public Vector3 up;

			[ReadOnly]
			public NativeArray<float> angleToPenalty;

			[ReadOnly]
			public NativeArray<float4> nodeNormals;

			public NativeArray<uint> penalty;

			public void Execute () {
            }
        }
	}
}
