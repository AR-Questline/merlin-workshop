namespace Pathfinding.Graphs.Grid.Rules {
	using Pathfinding.Jobs;
	using Unity.Jobs;
	using Unity.Collections;
	using Unity.Burst;
	using UnityEngine;
	using Unity.Mathematics;

	/// <summary>
	/// Applies penalty based on the elevation of the node.
	///
	/// This is useful if you for example want to discourage agents from walking high up in mountain regions.
	///
	/// The penalty applied is equivalent to:
	///
	/// <code>
	/// penalty = curve.evaluate(Mathf.Clamp01(Mathf.InverseLerp(lower elevation range, upper elevation range, elevation))) * penaltyScale
	/// </code>
	///
	/// [Open online documentation to see images]
	///
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[Pathfinding.Util.Preserve]
	public class RuleElevationPenalty : GridGraphRule {
		public float penaltyScale = 10000;
		public Vector2 elevationRange = new Vector2(0, 100);
		public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
		NativeArray<float> elevationToPenalty;

		public override void Register (GridGraphRules rules) {
        }

        public override void DisposeUnmanagedData()
        {
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
		public struct JobElevationPenalty : IJob {
			[ReadOnly]
			public NativeArray<float> elevationToPenalty;

			[ReadOnly]
			public NativeArray<Vector3> nodePositions;

			public Matrix4x4 worldToGraph;
			public NativeArray<uint> penalty;

			public void Execute () {
            }
        }
	}
}
